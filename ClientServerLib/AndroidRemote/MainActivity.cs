using System;
using System.Collections.Generic;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Provider;

using ClientServerLib;
using Vocaluxe.Base.Server;

namespace org.vocaluxe.app
{
	[Activity (Label = "Vocaluxe App", MainLauncher = true, ConfigurationChanges=Android.Content.PM.ConfigChanges.Orientation)]
	public class Activity1 : Activity
	{
		private readonly int REQUEST_TAKE_PICTURE = 1;
		private readonly int REQUEST_PICK_PICTURE = 2;

		CClient client = new CClient();
		CDiscover discover;
		Button bConnect;
		Button bSendAvatar;
		Button bSendProfile;

		TextView tIP;
		TextView tPassword;

		ProgressDialog pDialog;
		
		byte[] fileBytes;
		
		Java.IO.File file;
		Java.IO.File file2;
		Java.IO.File dir;
		System.IO.FileStream fs;

		bool sendProfile;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			SetContentView (Resource.Layout.Main);
			pDialog = new ProgressDialog (this);
			pDialog.SetCancelable (false);
			
			bConnect = FindViewById<Button> (Resource.Id.btConnect);
			bConnect.Click += delegate { Connect(); };

			Button bDiscover = FindViewById<Button> (Resource.Id.btDiscover);
			bDiscover.Click += delegate { Discover(); };

			bSendAvatar = FindViewById<Button> (Resource.Id.btSendAvatar);
			bSendAvatar.Click += SendAvatar;

			bSendProfile = FindViewById<Button> (Resource.Id.btSendProfile);
			bSendProfile.Click += SendProfile;

			tIP = FindViewById<TextView> (Resource.Id.tbIP);
			tPassword = FindViewById<TextView> (Resource.Id.tbPassword);

			sendProfile = false;

			discover = new CDiscover(3000, CCommands.BroadcastKeyword);
			Discover ();
		}
		
		protected override void OnStop ()
		{
			base.OnStop();
			//client.Close();
		}

		protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
		{
			//base.OnActivityResult(requestCode, resultCode, data);
			if (resultCode != Result.Ok)
				return;

			if (requestCode == REQUEST_TAKE_PICTURE)
			{
				// make it available in the gallery
				//Intent mediaScanIntent = new Intent(Intent.ActionMediaScannerScanFile);
				Android.Net.Uri contentUri = Android.Net.Uri.FromFile(file);
				//mediaScanIntent.SetData(contentUri);
				//SendBroadcast(mediaScanIntent);

				Java.IO.File dir2 = new Java.IO.File(Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryPictures), Resources.GetString(Resource.String.app_name));
				if (!dir.Exists())
				{
					dir.Mkdirs();
				}

				file2 = new Java.IO.File(dir, String.Format("myPhoto_{0}.jpg", Guid.NewGuid()));			

				Intent picker = new Intent ("com.android.camera.action.CROP");
				//Intent picker = new Intent (Intent.ActionGetContent, contentUri);
				picker.SetDataAndType (contentUri, "image/*");
				picker.PutExtra ("crop", "true");
				picker.PutExtra ("aspectX", 1);
				picker.PutExtra ("aspectY", 1);
				picker.PutExtra ("outputX", 512);
				picker.PutExtra ("outputY", 512);
				picker.PutExtra ("return-data", false);
				//picker.PutExtra ("outputFormat", Bitmap.CompressFormat.Jpeg.ToString ());
				picker.PutExtra ("noFaceDetection", true);
				picker.PutExtra (MediaStore.ExtraOutput, Android.Net.Uri.FromFile(file2));	
				StartActivityForResult (picker, REQUEST_PICK_PICTURE);
			}

			if (requestCode == REQUEST_PICK_PICTURE)
			{
				if (data.Extras != null) 
				{
					System.IO.FileInfo fi = new System.IO.FileInfo(file2.Path);
					fs = new System.IO.FileStream(file2.Path, System.IO.FileMode.Open);
					fileBytes = new byte[fi.Length];
					fs.BeginRead (fileBytes, 0, (int)fi.Length, new AsyncCallback(OnFileRead), null);
					fi = null;
				}
			}
		}


		private void OnFileRead(IAsyncResult ar)
		{
			if (fileBytes == null)
				return;

			fs.Flush();
			fs.Close();

			if (file != null)
				file.Delete ();

			if (file2 != null)
				file2.Delete ();

			if (sendProfile)
			{
				sendProfile = false;
				TextView tv = FindViewById<TextView>(Resource.Id.tbPlayerName);
				if (tv.Text == String.Empty)
					return;

				client.SendMessage (CCommands.CreateCommandSendProfile(fileBytes, tv.Text, 0), OnResponse);
			}
			else
				client.SendMessage (CCommands.CreateCommandSendAvatarPictureJpg(fileBytes), OnResponse);
		}

		private bool IsThereAnAppToTakePictures()
		{
			Intent intent = new Intent(MediaStore.ActionImageCapture);
			IList<ResolveInfo> availableActivities = PackageManager.QueryIntentActivities(intent, PackageInfoFlags.MatchDefaultOnly);
			return availableActivities != null && availableActivities.Count > 0;
		}

		private void SendAvatar(object sender, EventArgs eventArgs)
		{
			_TakePicture (false);
		}

		private void SendProfile(object sender, EventArgs eventArgs)
		{
			_TakePicture (true);
		}

		private void _TakePicture(bool SendProfile)
		{
			sendProfile = SendProfile;

			if (!IsThereAnAppToTakePictures())
				return;

			Intent intent = new Intent(MediaStore.ActionImageCapture);
			dir = new Java.IO.File(Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryPictures), Resources.GetString(Resource.String.app_name));
			if (!dir.Exists())
			{
				dir.Mkdirs();
			}

			file = new Java.IO.File(dir, String.Format("myPhoto_{0}.jpg", Guid.NewGuid()));			
			intent.PutExtra(MediaStore.ExtraOutput, Android.Net.Uri.FromFile(file));	
			StartActivityForResult(intent, REQUEST_TAKE_PICTURE);
		}
		
		private void Connect()
		{
			if (!client.Connected)
			{
				pDialog.SetMessage ("Connecting for Server...");
				pDialog.Show ();
				client.Connect(tIP.Text, 3000, tPassword.Text, OnConnectionChanged);
			}
			else
			{
				pDialog.Hide ();
				client.Disconnect();
			}
		}

		private void Discover()
		{
			pDialog.SetMessage ("Searching for Server...");
			pDialog.Show ();
			discover.Discover (_OnDiscovered);
		}
		
		private void OnConnectionChanged(bool Connected)
		{
			RunOnUiThread (() => UpdateConnectionStatus(Connected));
		}
		
		private void OnResponse(byte[] Message)
		{
			if (Message == null)
				return;
			
			if (Message.Length < 4)
				return;
			
			int command = BitConverter.ToInt32(Message, 0);
			switch (command)
			{
			case CCommands.ResponseOK:
				RunOnUiThread (() => Toast.MakeText(this, Resources.GetString(Resource.String.message_ok), ToastLength.Short).Show());
				break;

			case CCommands.ResponseNOK:
				RunOnUiThread (() => Toast.MakeText(this, Resources.GetString(Resource.String.message_nok), ToastLength.Short).Show());
				break;
				
			default:
				break;
			}
		}

		private void _OnDiscovered(string Address, string HostName)
		{
			RunOnUiThread (() => 
				{			    	
					if (Address != CDiscover.sTimeout && Address != CDiscover.sFinished)
					{
						tIP.Text = Address;
						Connect ();
					}
					else if (Address == CDiscover.sTimeout)
					{
						pDialog.Hide ();
						Toast.MakeText(this, "Cant't find any Vocaluxe Server", ToastLength.Short).Show();
					}
				});
		}

		private void UpdateConnectionStatus(bool Connected)
		{
			if (Connected)
			{
				pDialog.Hide ();
				bConnect.Text = Resources.GetString(Resource.String.button_disconnect);
				Toast.MakeText(this, Resource.String.message_connected, ToastLength.Short).Show();
				bSendAvatar.Enabled = true;
				bSendProfile.Enabled = true;
			}
			else
			{
				pDialog.Hide ();
				client.Disconnect();
				bConnect.Text = Resources.GetString(Resource.String.button_connect);
				bSendAvatar.Enabled = false;
				bSendProfile.Enabled = false;
				Toast.MakeText(this, Resources.GetString(Resource.String.message_disconnected), ToastLength.Short).Show();
			}
		}
	}
}


