﻿using System;
using System.Net;

namespace SharpRemote.Hosting
{
	/// <summary>
	/// Silo implementation not meant for production code as it uses the entire remoting chain without an actual need for it.
	/// </summary>
	public sealed class InProcessRemotingSilo
		: ISilo
	{
		private readonly SocketEndPoint _localEndPoint;
		private readonly SocketEndPoint _remoteEndPoint;
		private readonly ISubjectHost _subjectHost;

		public InProcessRemotingSilo()
		{
			const int subjectHostId = 0;

			_localEndPoint = new SocketEndPoint(IPAddress.Loopback);
			_subjectHost = _localEndPoint.CreateProxy<ISubjectHost>(subjectHostId);

			_remoteEndPoint = new SocketEndPoint(IPAddress.Loopback);
			_remoteEndPoint.CreateServant(subjectHostId, (ISubjectHost)new SubjectHost(_remoteEndPoint, subjectHostId+1, OnSubjectHostDisposed));

			_localEndPoint.Connect(_remoteEndPoint.LocalEndPoint, TimeSpan.FromSeconds(1));
		}

		private void OnSubjectHostDisposed()
		{
			
		}

		public TInterface CreateGrain<TInterface>(string assemblyQualifiedTypeName) where TInterface : class
		{
			return CreateGrain<TInterface>(Type.GetType(assemblyQualifiedTypeName));
		}

		public TInterface CreateGrain<TInterface>(Type implementation) where TInterface : class
		{
			var id = _subjectHost.CreateSubject(implementation, typeof (TInterface));
			var proxy = _localEndPoint.CreateProxy<TInterface>(id);
			return proxy;
		}

		public void Dispose()
		{
			_subjectHost.TryDispose();
			_localEndPoint.Dispose();
			_remoteEndPoint.Dispose();
		}
	}
}