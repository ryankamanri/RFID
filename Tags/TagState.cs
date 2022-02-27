using System;
using System.Collections.Generic;
using System.Text;

namespace RFID.Tags
{
	public enum TagState
	{
		ReadyState,
		ArbitrateState,
		ReplyState,
		AcknowledgedState,
		OpenState,
		SecuredState,
		KilledState
	}
}
