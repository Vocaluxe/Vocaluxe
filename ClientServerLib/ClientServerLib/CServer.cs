using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Web;

namespace ClientServerLib
{
    public class CServer
    {
        private ServiceHost host;
        private Uri baseAddress;

        private static SendKeyEventDelegate sendKeyEvent;
        public static SendKeyEventDelegate SendKeyEvent
        {
            internal get { return CServer.sendKeyEvent; }
            set { CServer.sendKeyEvent = value; }
        }

        #region profile

        private static GetProfileDataDelegate getProfileData;
        public static GetProfileDataDelegate GetProfileData
        {
            internal get { return CServer.getProfileData; }
            set { CServer.getProfileData = value; }
        }

        private static SendProfileDataDelegate sendProfileData;
        public static SendProfileDataDelegate SendProfileData
        {
            internal get { return CServer.sendProfileData; }
            set { CServer.sendProfileData = value; }
        }

        private static GetProfileListDelegate getProfileList;
        public static GetProfileListDelegate GetProfileList
        {
            internal get { return CServer.getProfileList; }
            set { CServer.getProfileList = value; }
        }


        #endregion

        #region photo
        private static SendPhotoDelegate sendPhoto;
        public static SendPhotoDelegate SendPhoto
        {
            get { return CServer.sendPhoto; }
            set { CServer.sendPhoto = value; }
        }

        #endregion

        #region website

        private static GetSiteFileDelegate getSiteFile;
        public static GetSiteFileDelegate GetSiteFile
        {
            internal get { return CServer.getSiteFile; }
            set { CServer.getSiteFile = value; }
        }

        #endregion

        public CServer(int port)
        {
            baseAddress = new Uri("http://localhost:" + port + "/");
            host = new WebServiceHost(typeof(CWebservice), baseAddress);
            WebHttpBinding wb = new WebHttpBinding();
            wb.MaxReceivedMessageSize = 10485760;
            wb.MaxBufferSize = 10485760;
            wb.MaxBufferPoolSize = 10485760;
            wb.ReaderQuotas.MaxStringContentLength = 10485760;
            wb.ReaderQuotas.MaxArrayLength = 10485760;
            wb.ReaderQuotas.MaxBytesPerRead = 10485760;
            host.AddServiceEndpoint(typeof(ICWebservice), wb, "");
        }

        public void Start()
        {
            try
            {
                // Step 4 Enable metadata exchange.
                ServiceMetadataBehavior smb = new ServiceMetadataBehavior();
                //smb.MetadataExporter.PolicyVersion = PolicyVersion.Policy15;
                //smb.HttpGetEnabled = true;
                host.Description.Behaviors.Add(smb);

                // Step 5 Start the service.
                host.Open();
            }
            catch (CommunicationException ce)
            {
                host.Abort();
            }
        }

        public void Stop()
        {
            try
            {
                host.Close();
            }
            catch (CommunicationException ce)
            {
                host.Abort();
            }
        }
    }
}
