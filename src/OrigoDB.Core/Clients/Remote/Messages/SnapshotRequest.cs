using System;

namespace OrigoDB.Core
{
	[Serializable]
	public class SnapshotRequest : NetworkMessage
	{
	}

	[Serializable]
	public class SnapshotResponse : NetworkMessage
	{
		public byte[] Snapshot { get { return (byte[]) Payload; } }
	}
}