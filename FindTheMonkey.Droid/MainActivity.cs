using System;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Views;
using Android.Widget;
using Android.OS;
using RadiusNetworks.IBeaconAndroid;
using Color = Android.Graphics.Color;
using Android.Support.V4.App;

namespace FindTheMonkey.Droid
{
	[Activity(Label = "Find The Beacon", MainLauncher = true, LaunchMode = LaunchMode.SingleTask)]
	public class MainActivity : Activity, IBeaconConsumer, View.IOnClickListener
	{
		private const string UUID = "B9407F30-F5F8-466E-AFF9-25556B57FE6D";
		private const string monkeyId = "Monkey";
		public Java.Lang.Integer minorValue = (Java.Lang.Integer)26227;

		bool _paused;
		View _view;
		IBeaconManager _iBeaconManager;
		MonitorNotifier _monitorNotifier;
		RangeNotifier _rangeNotifier;
		Region _monitoringRegion;
		Region _rangingRegion;
		TextView _text;
		EditText _input;
		Button _button;

		int _previousProximity;

		public MainActivity()
		{
			_iBeaconManager = IBeaconManager.GetInstanceForApplication(this);

			_monitorNotifier = new MonitorNotifier();
			_rangeNotifier = new RangeNotifier();

			//_monitoringRegion = new Region(monkeyId, UUID, null, null);
			_monitoringRegion = new Region(monkeyId, UUID, null, minorValue);
			_rangingRegion = new Region(monkeyId, UUID, null, minorValue);
		}

		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);

			SetContentView(Resource.Layout.Main);

			_view = FindViewById<RelativeLayout>(Resource.Id.findTheMonkeyView);
			_text = FindViewById<TextView>(Resource.Id.monkeyStatusLabel);
			_input = FindViewById<EditText>(Resource.Id.beaconInput);
			_button = FindViewById<Button>(Resource.Id.updateMinorButton);

			_iBeaconManager.Bind(this);

			_monitorNotifier.EnterRegionComplete += EnteredRegion;
			_monitorNotifier.ExitRegionComplete += ExitedRegion;

			_rangeNotifier.DidRangeBeaconsInRegionComplete += RangingBeaconsInRegion;

			_button.Click += delegate
			{
				RestartRangingForNewBeacon();
			};
		}

		void RestartRangingForNewBeacon()
		{
			_iBeaconManager.StopRangingBeaconsInRegion(_rangingRegion);
			_iBeaconManager.StopMonitoringBeaconsInRegion(_monitoringRegion);

			var input = FindViewById<EditText>(Resource.Id.beaconInput);
			var beaconInt = Convert.ToInt32(input.Text);
			Java.Lang.Integer convertedValue = (Java.Lang.Integer)beaconInt;
			minorValue = convertedValue;

			_rangingRegion = new Region(monkeyId, UUID, null, minorValue);
			_monitoringRegion = new Region(monkeyId, UUID, null, minorValue);

			_iBeaconManager.StartRangingBeaconsInRegion(_rangingRegion);
			_iBeaconManager.StartMonitoringBeaconsInRegion(_monitoringRegion);
		}

		protected override void OnResume()
		{
			base.OnResume();
			_paused = false;
		}

		protected override void OnPause()
		{
			base.OnPause();
			_paused = true;
		}

		void EnteredRegion(object sender, MonitorEventArgs e)
		{
			if(_paused)
			{
				ShowNotification();
			}
		}

		void ExitedRegion(object sender, MonitorEventArgs e)
		{
		}

		void RangingBeaconsInRegion(object sender, RangeEventArgs e)
		{
			if (e.Beacons.Count > 0)
			{
				var beacon = e.Beacons.FirstOrDefault();
				var message = string.Empty;

				switch ((ProximityType)beacon.Proximity)
				{
					case ProximityType.Immediate:
						UpdateDisplay("You found beacon " + beacon.Minor + "!", Color.DarkGreen);
						break;
					case ProximityType.Near:
						UpdateDisplay("Beacon " + beacon.Minor + ": You're getting warm", Color.DarkOrange);
						break;
					case ProximityType.Far:
						UpdateDisplay("Beacon " + beacon.Minor + ": You're freezing cold", Color.DarkBlue);
						break;
					case ProximityType.Unknown:
						UpdateDisplay("Beacon " + beacon.Minor + ": I'm not sure how close you are", Color.DarkRed);
						break;
				}

				_previousProximity = beacon.Proximity;
			}
		}

		#region IBeaconConsumer impl
		public void OnIBeaconServiceConnect()
		{
			_iBeaconManager.SetMonitorNotifier(_monitorNotifier);
			_iBeaconManager.SetRangeNotifier(_rangeNotifier);

			_iBeaconManager.StartMonitoringBeaconsInRegion(_monitoringRegion);
			_iBeaconManager.StartRangingBeaconsInRegion(_rangingRegion);
		}
		#endregion

		private void UpdateDisplay(string message, Color color)
		{
			RunOnUiThread(() =>
			{
				_text.Text = message;
				_view.SetBackgroundColor(color);
			});
		}

		private void ShowNotification()
		{
			var resultIntent = new Intent(this, typeof(MainActivity));
			resultIntent.AddFlags(ActivityFlags.ReorderToFront);
			var pendingIntent = PendingIntent.GetActivity(this, 0, resultIntent, PendingIntentFlags.UpdateCurrent);
			var notificationId = Resource.String.monkey_notification;

			var builder = new NotificationCompat.Builder(this)
				.SetSmallIcon(Resource.Drawable.Xamarin_Icon)
				.SetContentTitle(this.GetText(Resource.String.app_label))
				.SetContentText(this.GetText(Resource.String.monkey_notification))
				.SetContentIntent(pendingIntent)
				.SetAutoCancel(true);

			var notification = builder.Build();

			var notificationManager = (NotificationManager)GetSystemService(NotificationService);
			notificationManager.Notify(notificationId, notification);
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			_monitorNotifier.EnterRegionComplete -= EnteredRegion;
			_monitorNotifier.ExitRegionComplete -= ExitedRegion;

			_rangeNotifier.DidRangeBeaconsInRegionComplete -= RangingBeaconsInRegion;

			_iBeaconManager.StopMonitoringBeaconsInRegion(_monitoringRegion);
			_iBeaconManager.StopRangingBeaconsInRegion(_rangingRegion);
			_iBeaconManager.UnBind(this);
		}

		public void OnClick(View v)
		{
			throw new NotImplementedException();
		}
	}
}