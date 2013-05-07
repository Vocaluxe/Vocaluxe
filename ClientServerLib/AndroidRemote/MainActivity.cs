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

namespace AndroidRemote
{
	[Activity (Label = "Vocaluxe Remote", MainLauncher = true)]
	public class Activity1 : Activity
	{
		CClient client = new CClient();
		Button bConnect;
		Button bLogin;
		Button bUp;
		Button bDown;
		Button bSendAvatar;
		bool loggedIn;
		byte[] fileBytes;

		ImageView imageView;
		Java.IO.File file;
		Java.IO.File dir;
		System.IO.FileStream fs;
		
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
			
			loggedIn = false;
			
			// Set our view from the "main" layout resource
			SetContentView (Resource.Layout.Main);
			
			bConnect = FindViewById<Button> (Resource.Id.btConnect);
			bConnect.Click += delegate { Connect(); };
			
			bLogin = FindViewById<Button> (Resource.Id.btLogin);
			bLogin.Click += delegate { Login(); };
			
			bUp = FindViewById<Button> (Resource.Id.btUp);
			bUp.Click += delegate { 
				client.SendMessage (CCommands.CreateCommandWithoutParams (CCommands.CommandSendKeyUp), OnResponse); 
			};
			
			bDown = FindViewById<Button> (Resource.Id.btDown);
			bDown.Click += delegate { 
				client.SendMessage (CCommands.CreateCommandWithoutParams (CCommands.CommandSendKeyDown), OnResponse); 
			};

			imageView = FindViewById<ImageView>(Resource.Id.imageView1);

			bSendAvatar = FindViewById<Button> (Resource.Id.btSendAvatar);
			bSendAvatar.Click += SendAvatar;
		}
		
		protected override void OnStop ()
		{
			base.OnStop();
			//client.Close();
		}

		protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
		{
			base.OnActivityResult(requestCode, resultCode, data);
			
			// make it available in the gallery
			Intent mediaScanIntent = new Intent(Intent.ActionMediaScannerScanFile);
			Android.Net.Uri contentUri = Android.Net.Uri.FromFile(file);
			mediaScanIntent.SetData(contentUri);
			SendBroadcast(mediaScanIntent);
			System.IO.FileInfo fi = new System.IO.FileInfo(file.Path);
			fs = new System.IO.FileStream(file.Path, System.IO.FileMode.Open);
			fileBytes = new byte[fi.Length];
			fs.BeginRead (fileBytes, 0, (int)fi.Length, new AsyncCallback(OnFileRead), null);
		}

		private void OnFileRead(IAsyncResult ar)
		{
			if (fileBytes == null)
				return;

			fs.Flush();
			fs.Close();
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
			if (!IsThereAnAppToTakePictures())
				return;

			Intent intent = new Intent(MediaStore.ActionImageCapture);
			dir = new Java.IO.File(Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryPictures), "AndroidRemote");
			if (dir.Exists())
			{
				dir.Mkdirs();
			}

			file = new Java.IO.File(dir, String.Format("myPhoto_{0}.jpg", Guid.NewGuid()));			
			intent.PutExtra(MediaStore.ExtraOutput, Android.Net.Uri.FromFile(file));	
			StartActivityForResult(intent, 0);
		}
		
		private void Connect()
		{
			if (!client.Connected)
				client.Connect("192.168.178.57", 3000, OnConnectionChanged);
			else
				client.Disconnect();
		}
		
		private void Login()
		{
			if (!loggedIn)
				client.SendMessage (CCommands.CreateCommandLogin("vocaluxe"), OnResponse);
			else
				client.Disconnect();
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
			case CCommands.ResponseLoginFailed:
				RunOnUiThread (() => Toast.MakeText(this, "Login Failed", ToastLength.Short).Show());
				break;
				
			case CCommands.ResponseLoginWrongPassword:
				RunOnUiThread (() => Toast.MakeText(this, "Wrong Password", ToastLength.Short).Show());
				break;
				
			case CCommands.ResponseLoginOK:
				RunOnUiThread (() => UpdateLoginStatus(true));
				break;
				
			default:
				break;
			}
		}

		private void UpdateConnectionStatus(bool Connected)
		{
			if (Connected)
			{
				bLogin.Enabled = true;
				bConnect.Text = Resources.GetString(Resource.String.button_disconnect);
				Toast.MakeText(this, Resource.String.message_connected, ToastLength.Short).Show();
			}
			else
			{
				client.Disconnect();
				bLogin.Enabled = false;
				bLogin.Text = Resources.GetString(Resource.String.button_login);
				bConnect.Text = Resources.GetString(Resource.String.button_connect);
				bUp.Enabled = false;
				bDown.Enabled = false;
				Toast.MakeText(this, Resources.GetString(Resource.String.message_disconnected), ToastLength.Short).Show();
			}
		}
		
		private void UpdateLoginStatus(bool LoggedIn)
		{
			loggedIn = LoggedIn;

			if (LoggedIn)
			{
				bUp.Enabled = LoggedIn;
				bDown.Enabled = LoggedIn;
				bLogin.Text = Resources.GetString(Resource.String.button_logout);
				Toast.MakeText(this, "Logged In", ToastLength.Short).Show();
			}
			else
				Toast.MakeText(this, "Not Logged In", ToastLength.Short).Show();
		}
	}

	public static class BitmapHelpers
	{
		public static Bitmap LoadAndResizeBitmap(this string fileName, int width, int height)
		{
			// First we get the the dimensions of the file on disk
			BitmapFactory.Options options = new BitmapFactory.Options { InJustDecodeBounds = true };
			BitmapFactory.DecodeFile(fileName, options);
			
			// Next we calculate the ratio that we need to resize the image by
			// in order to fit the requested dimensions.
			int outHeight = options.OutHeight;
			int outWidth = options.OutWidth;
			int inSampleSize = 1;
			
			if (outHeight > height || outWidth > width)
			{
				inSampleSize = outWidth > outHeight
					? outHeight / height
						: outWidth / width;
			}
			
			// Now we will load the image and have BitmapFactory resize it for us.
			options.InSampleSize = inSampleSize;
			options.InJustDecodeBounds = false;
			Bitmap resizedBitmap = BitmapFactory.DecodeFile(fileName, options);
			return resizedBitmap;
		}
	}
}


