﻿using SharpAdbClient.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace SharpAdbClient.Tests
{
    [TestClass]
    public class AdbServerTests
    {
        private DummyAdbSocket socket;
        private DummyAdbCommandLineClient commandLineClient;

        [TestInitialize]
        public void Initialize()
        {
            this.socket = new DummyAdbSocket();
            Factories.AdbSocketFactory = (endPoint) => this.socket;

            this.commandLineClient = new DummyAdbCommandLineClient();
            Factories.AdbCommandLineClientFactory = (version) => this.commandLineClient;
        }

        [TestMethod]
        public void GetStatusNotRunningTest()
        {
            Factories.AdbSocketFactory = (endPoint) =>
            {
                throw new SocketException(AdbServer.ConnectionRefused);
            };

            var status = AdbServer.GetStatus();
            Assert.IsFalse(status.IsRunning);
            Assert.IsNull(status.Version);
        }

        [TestMethod]
        public void GetStatusRunningTest()
        {
            this.socket.Responses.Enqueue(AdbResponse.OK);
            this.socket.ResponseMessages.Enqueue("0020");

            var status = AdbServer.GetStatus();

            Assert.AreEqual(0, this.socket.Responses.Count);
            Assert.AreEqual(0, this.socket.ResponseMessages.Count);
            Assert.AreEqual(1, this.socket.Requests.Count);
            Assert.AreEqual("host:version", this.socket.Requests[0]);

            Assert.IsTrue(status.IsRunning);
            Assert.AreEqual(new Version(1, 0, 32), status.Version);
        }

        [TestMethod]
        [ExpectedException(typeof(SocketException))]
        public void GetStatusOtherSocketExceptionTest()
        {
            Factories.AdbSocketFactory = (endPoint) =>
            {
                throw new SocketException();
            };

            var status = AdbServer.GetStatus();
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void GetStatusOtherExceptionTest()
        {
            Factories.AdbSocketFactory = (endPoint) =>
            {
                throw new Exception();
            };

            var status = AdbServer.GetStatus();
        }

        [TestMethod]
        public void StartServerAlreadyRunningTest()
        {
            this.socket.Responses.Enqueue(AdbResponse.OK);
            this.socket.ResponseMessages.Enqueue("0020");

            var result = AdbServer.StartServer(null, false);

            Assert.AreEqual(StartServerResult.AlreadyRunning, result);

            Assert.AreEqual(1, this.socket.Requests.Count);
            Assert.AreEqual("host:version", this.socket.Requests[0]);
        }

        [TestMethod]
        [ExpectedException(typeof(AdbException))]
        public void StartServerOutdatedRunningNoExecutableTest()
        {
            this.socket.Responses.Enqueue(AdbResponse.OK);
            this.socket.ResponseMessages.Enqueue("0010");

            var result = AdbServer.StartServer(null, false);

            Assert.AreEqual(1, this.socket.Requests.Count);
            Assert.AreEqual("host:version", this.socket.Requests[0]);
        }

        [TestMethod]
        [ExpectedException(typeof(AdbException))]
        public void StartServerNotRunningNoExecutableTest()
        {
            Factories.AdbSocketFactory = (endPoint) =>
            {
                throw new SocketException(AdbServer.ConnectionRefused);
            };

            var result = AdbServer.StartServer(null, false);
        }

        [TestMethod]
        public void StartServerOutdatedRunningTest()
        {
            this.socket.Responses.Enqueue(AdbResponse.OK);
            this.socket.ResponseMessages.Enqueue("0010");

            this.commandLineClient.Version = new Version(1, 0, 32);

            Assert.IsFalse(this.commandLineClient.ServerStarted);

            var result = AdbServer.StartServer("adb.exe", false);

            Assert.IsTrue(this.commandLineClient.ServerStarted);

            Assert.AreEqual(2, this.socket.Requests.Count);
            Assert.AreEqual("host:version", this.socket.Requests[0]);
            Assert.AreEqual("host:kill", this.socket.Requests[1]);
        }

        [TestMethod]
        public void StartServerNotRunningTest()
        {
            Factories.AdbSocketFactory = (endPoint) =>
            {
                throw new SocketException(AdbServer.ConnectionRefused);
            };

            this.commandLineClient.Version = new Version(1, 0, 32);

            Assert.IsFalse(this.commandLineClient.ServerStarted);

            var result = AdbServer.StartServer("adb.exe", false);

            Assert.IsTrue(this.commandLineClient.ServerStarted);
        }

        [TestMethod]
        public void StartServerIntermediateRestartRequestedRunningTest()
        {
            this.socket.Responses.Enqueue(AdbResponse.OK);
            this.socket.ResponseMessages.Enqueue("001f");

            this.commandLineClient.Version = new Version(1, 0, 32);

            Assert.IsFalse(this.commandLineClient.ServerStarted);

            var result = AdbServer.StartServer("adb.exe", true);

            Assert.IsTrue(this.commandLineClient.ServerStarted);

            Assert.AreEqual(2, this.socket.Requests.Count);
            Assert.AreEqual("host:version", this.socket.Requests[0]);
            Assert.AreEqual("host:kill", this.socket.Requests[1]);
        }

        [TestMethod]
        public void StartServerIntermediateRestartNotRequestedRunningTest()
        {
            this.socket.Responses.Enqueue(AdbResponse.OK);
            this.socket.ResponseMessages.Enqueue("001f");

            this.commandLineClient.Version = new Version(1, 0, 32);

            Assert.IsFalse(this.commandLineClient.ServerStarted);

            var result = AdbServer.StartServer("adb.exe", false);

            Assert.IsFalse(this.commandLineClient.ServerStarted);

            Assert.AreEqual(1, this.socket.Requests.Count);
            Assert.AreEqual("host:version", this.socket.Requests[0]);
        }
    }
}
