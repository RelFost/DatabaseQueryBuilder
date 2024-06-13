#if CARBON

using System;
using API.Assembly;
using Carbon;

namespace Extension
{
	public class ExtensionEntrypoint : ICarbonExtension
	{
		public void OnLoaded(EventArgs args)
		{
			Community.Runtime.Events.Subscribe(API.Events.CarbonEvent.OnServerInitialized, arg =>
			{
				try
				{
					// Do something wild
				}
				catch (Exception ex)
				{
					Logger.Error("Failed doing something wild.", ex);
				}
			});
		}

		public void Awake(EventArgs args)
		{
			// Do something wild
		}

		public void OnUnloaded(EventArgs args)
		{
			// Do something wild
		}
	}
}

#endif
