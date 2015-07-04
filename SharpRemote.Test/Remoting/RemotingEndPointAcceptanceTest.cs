﻿using System;
using System.ComponentModel;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SharpRemote.Hosting;
using SharpRemote.Test.Hosting;
using SharpRemote.Test.Types;
using SharpRemote.Test.Types.Exceptions;
using SharpRemote.Test.Types.Interfaces;
using SharpRemote.Test.Types.Interfaces.PrimitiveTypes;
using log4net.Core;
using Description = NUnit.Framework.DescriptionAttribute;

namespace SharpRemote.Test.Remoting
{
	[TestFixture]
	[Description(
		"Verifies the behaviour of two connected RemotingEndPoint instances regarding successful (in terms of the connection) behaviour"
		)]
	public class RemotingEndPointAcceptanceTest
	{
		private SocketRemotingEndPoint _server;
		private SocketRemotingEndPoint _client;

		protected SocketRemotingEndPoint CreateEndPoint(string name = null)
		{
			return new SocketRemotingEndPoint(name);
		}

		[TestFixtureSetUp]
		public void SetUp()
		{
			TestLogger.EnableConsoleLogging(Level.Error);
			TestLogger.SetLevel<SocketRemotingEndPoint>(Level.Info);

			_server = CreateEndPoint("Server");
			_server.Bind(IPAddress.Loopback);

			_client = CreateEndPoint("Client");
			_client.Connect(_server.LocalEndPoint, TimeSpan.FromMinutes(1));
		}

		[TestFixtureTearDown]
		public void TearDown()
		{
			_server.TryDispose();
			_client.TryDispose();
		}

		[Test]
		public void TestCreateSubject()
		{
			var subject = new Mock<ISubjectHost>();

			Type type = null;
			Type @interface = null;
			const int id = 42;

			subject.Setup(x => x.CreateSubject1(It.IsAny<Type>(), It.IsAny<Type>()))
			       .Returns((Type a, Type b) =>
				       {
					       type = a;
					       @interface = b;
					       return id;
				       });

			const int servantId = 10;
			IServant servant = _server.CreateServant(servantId, subject.Object);
			var proxy = _client.CreateProxy<ISubjectHost>(servantId);

			ulong actualId = proxy.CreateSubject1(typeof (GetStringPropertyImplementation), typeof (IGetStringProperty));
			actualId.Should().Be(42);
			type.Should().Be<GetStringPropertyImplementation>();
			@interface.Should().Be<IGetStringProperty>();
		}

		[Test]
		[Description("")]
		public void TestGetNonStartedTaskIsNotSupported1()
		{
			const int servantId = 14;
			var subject = new Mock<IReturnsTask>();
			subject.Setup(x => x.DoStuff()).Returns(() => new Task(() => { }));
			IServant servant = _server.CreateServant(servantId, subject.Object);
			var proxy = _client.CreateProxy<IReturnsTask>(servantId);
			new Action(() => proxy.DoStuff().Wait())
				.ShouldThrow<NotSupportedException>()
				.WithMessage("IReturnsTask.DoStuff of servant #14 returned a non-started task - this is not supported");
		}

		[Test]
		[Description("")]
		public void TestGetNonStartedTaskIsNotSupported2()
		{
			const int servantId = 15;
			var subject = new Mock<IReturnsIntTask>();
			subject.Setup(x => x.DoStuff()).Returns(() => new Task<int>(() => 42));
			IServant servant = _server.CreateServant(servantId, subject.Object);
			var proxy = _client.CreateProxy<IReturnsIntTask>(servantId);
			new Action(() => proxy.DoStuff().Wait())
				.ShouldThrow<NotSupportedException>()
				.WithMessage("IReturnsIntTask.DoStuff of servant #15 returned a non-started task - this is not supported");
		}

		[Test]
		public void TestGetProperty()
		{
			var subject = new Mock<IGetDoubleProperty>();
			subject.Setup(x => x.Value).Returns(42);

			const int servantId = 1;
			IServant servant = _server.CreateServant(servantId, subject.Object);
			var proxy = _client.CreateProxy<IGetDoubleProperty>(servantId);
			proxy.Value.Should().Be(42);
		}

		[Test]
		[Description("Verifies that an eception can be marshalled")]
		public void TestGetPropertyThrowException1()
		{
			var subject = new Mock<IGetDoubleProperty>();
			subject.Setup(x => x.Value).Returns(() => { throw new ArgumentException("Foobar"); });

			const int servantId = 2;
			IServant servant = _server.CreateServant(servantId, subject.Object);
			var proxy = _client.CreateProxy<IGetDoubleProperty>(servantId);
			new Action(() => { double unused = proxy.Value; })
				.ShouldThrow<ArgumentException>()
				.WithMessage("Foobar");
		}

		[Test]
		[Description(
			"Verifies that if an exception could not be serialized, but can be re-constructed due to a default ctor, then it is thrown again"
			)]
		public void TestGetPropertyThrowNonSerializableException()
		{
			var subject = new Mock<IGetDoubleProperty>();
			subject.Setup(x => x.Value).Returns(() => { throw new NonSerializableExceptionButDefaultCtor(); });

			const int servantId = 3;
			IServant servant = _server.CreateServant(servantId, subject.Object);
			var proxy = _client.CreateProxy<IGetDoubleProperty>(servantId);
			new Action(() => { double unused = proxy.Value; })
				.ShouldThrow<UnserializableException>();
		}

		[Test]
		public void TestGetTaskContinueWith()
		{
			const int servantId = 13;
			var subject = new Mock<IReturnsIntTask>();
			subject.Setup(x => x.DoStuff()).Returns(() => Task<int>.Factory.StartNew(() => 42));
			IServant servant = _server.CreateServant(servantId, subject.Object);
			var proxy = _client.CreateProxy<IReturnsIntTask>(servantId);
			int? result = null;
			Task task = proxy.DoStuff().ContinueWith(unused => { result = unused.Result; });
			task.Wait();
			result.Should().Be(42);
		}

		[Test]
		[Description("Verifies that the exception thrown by a task is correctly marshalled")]
		public void TestGetTaskThrowException1()
		{
			const int servantId = 11;
			var subject = new Mock<IReturnsTask>();
			subject.Setup(x => x.DoStuff()).Returns(() => Task.Factory.StartNew(() => { throw new Win32Exception(1337); }));
			IServant servant = _server.CreateServant(servantId, subject.Object);
			var proxy = _client.CreateProxy<IReturnsTask>(servantId);
			Task task = proxy.DoStuff();
			new Action(task.Wait)
				.ShouldThrow<AggregateException>();
		}

		[Test]
		[Description("Verifies that the exception thrown by a task is correctly marshalled")]
		public void TestGetTaskThrowException2()
		{
			const int servantId = 12;
			var subject = new Mock<IReturnsIntTask>();
			subject.Setup(x => x.DoStuff()).Returns(() => Task<int>.Factory.StartNew(() => { throw new Win32Exception(1337); }));
			IServant servant = _server.CreateServant(servantId, subject.Object);
			var proxy = _client.CreateProxy<IReturnsIntTask>(servantId);
			Task<int> task = proxy.DoStuff();
			new Action(task.Wait)
				.ShouldThrow<AggregateException>();
		}

		[Test]
		[Description("Verifies that creating a proxy with the wrong type doesn't throw")]
		public void TestInterfaceTypeMismatch1()
		{
			var subject = new Mock<IReturnsIntTask>();
			const int objectId = 16;
			var servant = _server.CreateServant(objectId, subject.Object);
			new Action(() => _client.CreateProxy<IReturnsTask>(objectId))
				.ShouldNotThrow("Because creating proxy & servant of different type is not wrong, until a method is invoked");
		}

		[Test]
		[Description("Verifies that invoking a method on a proxy/servant type mismatch throws")]
		public void TestInterfaceTypeMismatch2()
		{
			var subject = new Mock<IReturnsIntTask>();
			const int objectId = 17;
			var servant = _server.CreateServant(objectId, subject.Object);
			var proxy = _client.CreateProxy<IReturnsTask>(objectId);
			new Action(() => proxy.DoStuff().Wait())
				.ShouldThrow<TypeMismatchException>();
		}

		[Test]
		[Description("Verifies that an interface which itself implements another interface works")]
		public void TestMultipleInterfaces()
		{
			var subject = new Mock<ICalculator>();
			bool isDisposed = false;
			subject.Setup(x => x.IsDisposed).Returns(() => isDisposed);
			subject.Setup(x => x.Dispose()).Callback(() => isDisposed = true);
			subject.Setup(x => x.Add(It.IsAny<double>(), It.IsAny<double>()))
			       .Returns((double x, double y) => x + y);

			const int servantId = 9;
			IServant servant = _server.CreateServant(servantId, subject.Object);
			var proxy = _client.CreateProxy<ICalculator>(servantId);

			proxy.Add(1, 2).Should().Be(3);
			proxy.Add(5, 42).Should().Be(47);
			proxy.IsDisposed.Should().BeFalse();
			proxy.Dispose();
			proxy.IsDisposed.Should().BeTrue();
		}

		[Test]
		[Description(
			"Verifies that raising an event on the subject to which no-one is connected via the proxy doesn't do anything - besides not failing"
			)]
		public void TestRaiseEmptyEvent()
		{
			var subject = new Mock<IEventInt32>();
			const int servantId = 4;
			IServant servant = _server.CreateServant(servantId, subject.Object);
			var proxy = (IProxy) _client.CreateProxy<IEventInt32>(servantId);

			new Action(() => subject.Raise(x => x.Foobar += null, 42))
				.ExecutionTime().ShouldNotExceed(TimeSpan.FromSeconds(1));

			servant.ObjectId.Should().Be(servantId);
			proxy.ObjectId.Should().Be(servantId);
		}

		[Test]
		[Description(
			"Verifies that raising an event on the subject successfully serialized the parameter's value and forwards it to the proxy"
			)]
		public void TestRaiseEvent1()
		{
			var subject = new Mock<IEventInt32>();
			const int servantId = 5;
			IServant servant = _server.CreateServant(servantId, subject.Object);
			var proxy = _client.CreateProxy<IEventInt32>(servantId);

			int? actualValue = null;
			proxy.Foobar += x => actualValue = x;

			const int value = 42;
			new Action(() => subject.Raise(x => x.Foobar += null, value))
				.ExecutionTime().ShouldNotExceed(TimeSpan.FromSeconds(1));

			actualValue.Should().Be(value);
		}

		[Test]
		[Description("Verifies that delegates are invoked in the exact order that they are registered in")]
		public void TestRaiseEvent2()
		{
			var subject = new Mock<IEventInt32>();
			const int servantId = 6;
			IServant servant = _server.CreateServant(servantId, subject.Object);
			var proxy = _client.CreateProxy<IEventInt32>(servantId);

			int? actualValue1 = null;
			int? actualValue2 = null;
			proxy.Foobar += x => actualValue1 = x;
			proxy.Foobar += x => { throw new ArgumentOutOfRangeException("value"); };
			proxy.Foobar += x => actualValue2 = x;

			const int value = 42;
			new Action(() => subject.Raise(x => x.Foobar += null, value))
				.ShouldThrow<ArgumentOutOfRangeException>()
				.WithMessage("Specified argument was out of the range of valid values.\r\nParameter name: value");

			actualValue1.Should().Be(value);
			actualValue2.Should().NotHaveValue();
		}

		[Test]
		[Description("Verifies that a delegate is no longer invoked once it's removed from the event")]
		public void TestRaiseEvent3()
		{
			var subject = new Mock<IEventInt32>();
			const int servantId = 7;
			IServant servant = _server.CreateServant(servantId, subject.Object);
			var proxy = _client.CreateProxy<IEventInt32>(servantId);

			int? actualValue = null;
			Action<int> fn = x => actualValue = x;
			proxy.Foobar += fn;

			const int value1 = 9001;
			new Action(() => subject.Raise(x => x.Foobar += null, value1))
				.ExecutionTime().ShouldNotExceed(TimeSpan.FromSeconds(1));
			actualValue.Should().Be(value1);

			proxy.Foobar -= fn;
			const int value2 = int.MaxValue;
			new Action(() => subject.Raise(x => x.Foobar += null, value2))
				.ExecutionTime().ShouldNotExceed(TimeSpan.FromSeconds(1));
			actualValue.Should().Be(value1);
		}

		[Test]
		[Description(
			"Verifies that an exception is successfully marshalled when thrown by the delegate attached to the proxie's event")]
		public void TestRaiseEventThrowException1()
		{
			var subject = new Mock<IEventInt32>();
			const int servantId = 8;
			IServant servant = _server.CreateServant(servantId, subject.Object);
			var proxy = _client.CreateProxy<IEventInt32>(servantId);

			proxy.Foobar += x => { throw new ArgumentOutOfRangeException("value"); };

			const int value = 42;
			new Action(() => subject.Raise(x => x.Foobar += null, value))
				.ShouldThrow<ArgumentOutOfRangeException>()
				.WithMessage("Specified argument was out of the range of valid values.\r\nParameter name: value");
		}
	}
}