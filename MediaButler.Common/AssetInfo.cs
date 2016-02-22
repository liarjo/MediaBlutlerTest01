using Microsoft.WindowsAzure.MediaServices.Client;
using Microsoft.WindowsAzure.MediaServices.Client.DynamicEncryption;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaButler.Common
{
    public enum PublishStatus
    {
        NotPublished = 0,
        PublishedActive = 1,
        PublishedFuture = 2,
        PublishedExpired = 3,
    }

    
    public class AssetInfo
    {
        private List<IAsset> SelectedAssets;
        public const string Type_Blueprint = "Blueprint";
        public const string Type_Empty = "(empty)";
        public const string _prog_down_https = "Progressive Download URIs (https)";
        public const string _prog_down_http = "Progressive Download URIs (http)";
        public const string _hls_v4 = "HLS v4  URI";
        public const string _hls_v3 = "HLS v3  URI";
        public const string _dash = "MPEG-DASH URI";
        public const string _smooth = "Smooth Streaming URI";
        public const string _smooth_legacy = "Smooth Streaming (legacy) URI";
        public const string _hls = "HLS URI";

        private const string format_smooth_legacy = "fmp4-v20";
        private const string format_hls_v4 = "m3u8-aapl";
        private const string format_hls_v3 = "m3u8-aapl-v3";
        private const string format_dash = "mpd-time-csf";
        private const string format_url = "(format={0})";

        public AssetInfo(List<IAsset> MySelectedAssets)
        {
            SelectedAssets = MySelectedAssets;
        }
        public AssetInfo(IAsset asset)
        {
            SelectedAssets = new List<IAsset>();
            SelectedAssets.Add(asset);
        }


        public static string GetSmoothLegacy(string smooth_uri)
        {
            return string.Format("{0}(format={1})", smooth_uri, format_smooth_legacy);
        }

        public static string GetHLSv3(string hls_uri)
        {
            return hls_uri.Replace("(format=" + format_hls_v4, "(format=" + format_hls_v3);
        }
        public long GetSize()
        {
            return GetSize(0);
        }

        public long GetSize(int index)
        {
            if (index >= SelectedAssets.Count) return -1;

            long size = 0;
            foreach (IAssetFile objFile in SelectedAssets[index].AssetFiles)
            { size += objFile.ContentFileSize; }
            return size;
        }

        public static long GetSize(IAsset asset)
        {

            long size = 0;
            foreach (IAssetFile objFile in asset.AssetFiles)
            { size += objFile.ContentFileSize; }
            return size;
        }



        public static string GetDynamicEncryptionType(IAsset asset)
        {
            if (asset.DeliveryPolicies.Count > 0)
            {
                string str = string.Empty;
                switch (asset.DeliveryPolicies.FirstOrDefault().AssetDeliveryPolicyType)
                {
                    case AssetDeliveryPolicyType.Blocked:
                        str = "Blocked";
                        break;
                    case AssetDeliveryPolicyType.DynamicCommonEncryption:
                        str = "CENC";
                        break;
                    case AssetDeliveryPolicyType.DynamicEnvelopeEncryption:
                        str = "AES";
                        break;
                    case AssetDeliveryPolicyType.NoDynamicEncryption:
                        str = "No";
                        break;
                    case AssetDeliveryPolicyType.None:
                    default:
                        str = string.Empty;
                        break;

                }
                return str;
            }
            else
            {
                return string.Empty;
            }
        }


        public PublishStatus GetPublishedStatus(LocatorType LocType)
        {
            PublishStatus LocPubStatus;

            // if there is one locato for this type
            if ((SelectedAssets.FirstOrDefault().Locators.Where(l => l.Type == LocType).Count() > 0))
            {
                if (!SelectedAssets.FirstOrDefault().Locators.Where(l => (l.Type == LocType)).All(l => (l.ExpirationDateTime < DateTime.UtcNow)))
                {// not all int the past
                    var query = SelectedAssets.FirstOrDefault().Locators.Where(l => ((l.Type == LocType) && (l.ExpirationDateTime > DateTime.UtcNow) && (l.StartTime != null)));
                    // if no locator are valid today but at least one will in the future
                    if (query.ToList().Count() > 0)
                    {
                        LocPubStatus = (query.All(l => (l.StartTime > DateTime.UtcNow))) ? PublishStatus.PublishedFuture : PublishStatus.PublishedActive;
                    }
                    else
                    {
                        LocPubStatus = PublishStatus.PublishedActive;
                    }
                }
                else      // if all locators are in the past
                {
                    LocPubStatus = PublishStatus.PublishedExpired;
                }
            }
            else
            {
                LocPubStatus = PublishStatus.NotPublished;
            }

            return LocPubStatus;

        }

        public static PublishStatus GetPublishedStatusForLocator(ILocator Locator)
        {
            PublishStatus LocPubStatus;
            if (!(Locator.ExpirationDateTime < DateTime.UtcNow))
            {// not in the past
                // if  locator is not valid today but will be in the future
                if (Locator.StartTime != null)
                {
                    LocPubStatus = (Locator.StartTime > DateTime.UtcNow) ? PublishStatus.PublishedFuture : PublishStatus.PublishedActive;
                }
                else
                {
                    LocPubStatus = PublishStatus.PublishedActive;
                }
            }
            else      // if locator is in the past
            {
                LocPubStatus = PublishStatus.PublishedExpired;
            }
            return LocPubStatus;
        }


        public static string GetAssetType(IAsset asset)
        {
            string type = asset.AssetType.ToString();
            int assetcount = asset.AssetFiles.Count();
            int number = assetcount;

            switch (asset.AssetType)
            {
                case AssetType.MediaServicesHLS:
                    type = "Media Services HLS";
                    break;

                case AssetType.MP4:
                    break;

                case AssetType.MultiBitrateMP4:
                    type = "Multi Bitrate MP4";
                    break;

                case AssetType.SmoothStreaming:
                    type = "Smooth Streaming";
                    break;

                case AssetType.Unknown:
                    string ext;
                    string pr = string.Empty;

                    if (assetcount == 0) return "(empty)";

                    if (assetcount == 1)
                    {
                        number = 1;
                        ext = Path.GetExtension(asset.AssetFiles.FirstOrDefault().Name.ToUpper());
                        if (!string.IsNullOrEmpty(ext)) ext = ext.Substring(1);
                        switch (ext)
                        {
                            case "KAYAK":
                            case "XENIO":
                                type = Type_Blueprint;
                                break;

                            default:
                                type = ext;
                                break;
                        }
                    }
                    else
                    { // multi files in asset
                        var AssetFiles = asset.AssetFiles.ToList();
                        var JPGAssetFiles = AssetFiles.Where(f => f.Name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) | f.Name.EndsWith(".png", StringComparison.OrdinalIgnoreCase) | f.Name.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase) | f.Name.EndsWith(".gif", StringComparison.OrdinalIgnoreCase)).ToArray();

                        if ((JPGAssetFiles.Count() > 1) && (JPGAssetFiles.Count() == AssetFiles.Count))
                        {
                            type = "Thumbnails";
                            number = JPGAssetFiles.Count();
                        }
                    }
                    break;

                default:
                    break;

            }
            return string.Format("{0} ({1})", type, number);
        }

        private static long ListFilesInAsset(IAsset asset, ref StringBuilder builder)
        {
            // Display the files associated with each asset. 

            long assetSize = 0;
            foreach (IAssetFile fileItem in asset.AssetFiles)
            {
                if (fileItem.IsPrimary) builder.AppendLine("Primary");
                builder.AppendLine("Name: " + fileItem.Name);
                builder.AppendLine("Size: " + fileItem.ContentFileSize + " Bytes");
                assetSize += fileItem.ContentFileSize;
                builder.AppendLine("==============");
            }
            return assetSize;
        }

        public static String FormatByteSize(long? byteCountl)
        {
            if (byteCountl.HasValue == true)
            {
                long byteCount = (long)byteCountl;
                string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
                if (byteCount == 0)
                    return "0 " + suf[0];
                long bytes = Math.Abs(byteCount);
                int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
                double num = Math.Round(bytes / Math.Pow(1024, place), 1);
                return (Math.Sign(byteCount) * num).ToString() + " " + suf[place];
            }
            else return null;
        }
        public  string GetJsonAssetFilesInfo()
        {
            string aux="";
            //if (SelectedAssets.Count > 0)
            //{
            //    List<dynamic> myAssets = new List<dynamic>();

            //    List<dynamic> myFiles = new List<dynamic>();

            //    foreach (IAsset theAsset in SelectedAssets)
            //    {
            //        dynamic assetX = new JObject();
            //        assetX.Id = theAsset.Id;
            //        assetX.Uri= theAsset.Uri;
            //        assetX.AlternateId= theAsset.AlternateId;
            //        //Assets Files
            //        List<string> filesX = new List<string>();
            //        foreach (var assetFile in theAsset.AssetFiles)
            //        {
            //            dynamic fileX = new JObject();
            //            fileX.Name = assetFile.Name;
            //            fileX.ContentFileSize= assetFile.ContentFileSize;
            //            fileX.MimeType= assetFile.MimeType;
            //            filesX.Add(fileX.ToString());
            //        }
            //        myFiles.Add(fileX);
            //        assetX.AssetFiles = Newtonsoft.Json.JsonConvert.SerializeObject(filesX);

            //        myAssets.Add(assetX.ToString());
            //    }
                
            //       aux = Newtonsoft.Json.JsonConvert.SerializeObject(myAssets);
            //}
            //else
            //{

            //}


                return aux;
        }
        public string GetStatJson()
        {
            Hashtable allInfo = new Hashtable();
            if (SelectedAssets.Count > 0)
            {
                // Asset Stats
                foreach (IAsset theAsset in SelectedAssets)
                {
                    Hashtable AssetDetail = new Hashtable();
                    string MyAssetType = AssetInfo.GetAssetType(theAsset);
                    bool bfileinasset = (theAsset.AssetFiles.Count() == 0) ? false : true;
                    long size = -1;
                    if (bfileinasset)
                    {
                        size = 0;
                        foreach (IAssetFile file in theAsset.AssetFiles)
                        {
                            size += file.ContentFileSize;
                        }
                    }
                    
                    AssetDetail.Add("Asset Name",theAsset.Name);
                    AssetDetail.Add("Asset Type", theAsset.AssetType);
                    AssetDetail.Add("Asset Id", theAsset.Id);
                    AssetDetail.Add("Alternate ID", theAsset.AlternateId);
                    if (size != -1)
                        AssetDetail.Add("Size", FormatByteSize(size));
                    else
                        AssetDetail.Add("Size", FormatByteSize(0));

                    AssetDetail.Add("State", theAsset.State);
                    AssetDetail.Add("Created", theAsset.Created.ToLongDateString() + " " + theAsset.Created.ToLongTimeString());
                    AssetDetail.Add("Last Modified", theAsset.LastModified.ToLongDateString() + " " + theAsset.LastModified.ToLongTimeString());
                    AssetDetail.Add("Creations Options", theAsset.Options);
                    

                    if (theAsset.State != AssetState.Deleted)
                    {
                        AssetDetail.Add("IsStreamable", theAsset.IsStreamable);
                        AssetDetail.Add("SupportsDynEnc", theAsset.SupportsDynamicEncryption);
                        AssetDetail.Add("Uri", theAsset.Uri.ToString());
                        AssetDetail.Add("Storage Name", theAsset.StorageAccountName);
                        AssetDetail.Add("Storage Bytes used", FormatByteSize(theAsset.StorageAccount.BytesUsed));
                        AssetDetail.Add("Storage IsDefault", theAsset.StorageAccount.IsDefault);

                        Hashtable ParentAssets = new Hashtable();
                        foreach (IAsset p_asset in theAsset.ParentAssets)
                        {
                            ParentAssets.Add(p_asset.Id, p_asset.Name);
                           
                        }
                        AssetDetail.Add("ParentAsset",ParentAssets);

                        Hashtable ContentKeys = new Hashtable();
                        foreach (IContentKey key in theAsset.ContentKeys)
                        {
                            string[] ContentKey = new string[3] {key.Name, key.Id,key.ContentKeyType.ToString()};
                            ContentKeys.Add(key.Id, ContentKey);
                         
                        }
                        AssetDetail.Add("ContentKeys", ContentKeys);

                        Hashtable DeliveryPolicies = new Hashtable();
                        foreach (var pol in theAsset.DeliveryPolicies)
                        {
                            string[] DeliveryPolicie = new string[4] 
                                {
                                    pol.Name, 
                                    pol.Id, 
                                    pol.AssetDeliveryPolicyType.ToString(),
                                    pol.AssetDeliveryProtocol.ToString() 
                                };
                            DeliveryPolicies.Add(pol.Id, DeliveryPolicie);
                        }
                        AssetDetail.Add("DeliveryPolicies",DeliveryPolicies);

                        Hashtable AssetFiles = new Hashtable();
                        foreach (IAssetFile fileItem in theAsset.AssetFiles)
                        {
                            string defaultTxt;
                            if (fileItem.IsPrimary) 
                                defaultTxt="True";
                            else
                                defaultTxt="False";
                            string[] xAssetFile = 
                                new string[14] { defaultTxt,fileItem.Name, 
                                    fileItem.Id,
                                    fileItem.ContentFileSize + " Bytes",
                                    fileItem.MimeType,
                                    fileItem.InitializationVector,
                                    fileItem.Created.ToString(),
                                    fileItem.LastModified.ToString(),
                                    fileItem.IsEncrypted.ToString(),
                                    fileItem.EncryptionScheme,
                                    fileItem.EncryptionVersion,
                                    fileItem.EncryptionKeyId,
                                    fileItem.InitializationVector,
                                    fileItem.ParentAssetId
                                };
                            AssetFiles.Add(fileItem.Id, xAssetFile);
                        }
                        AssetDetail.Add("AssetFiles", AssetFiles);

                        var assetFilesALL = theAsset.AssetFiles.ToList();
                        Hashtable Locators = new Hashtable();
                        foreach (ILocator locator in theAsset.Locators)
                        {
                            string StartTime="";
                            if (locator.StartTime != null) 
                                StartTime=((DateTime)locator.StartTime).ToLongDateString() + " " + ((DateTime)locator.StartTime).ToLongTimeString();
                            string    ExpirationDateTime="";
                            if (locator.ExpirationDateTime != null) 
                                ExpirationDateTime= ((DateTime)locator.ExpirationDateTime).ToLongDateString() + " " + ((DateTime)locator.ExpirationDateTime).ToLongTimeString();
                        
                            string[] Locator = new string[8] {
                            locator.Name,
                            locator.Type.ToString(),
                             locator.Id,
                             locator.Path,
                             StartTime,
                             ExpirationDateTime,
                             "{}", //OnDemandOrigin
                             "{}"  //Sas origin
                            };

                            if (locator.Type == LocatorType.OnDemandOrigin)
                            {
                                string[] OnDemandOrigin = new string[4];
                                var ismfile = assetFilesALL.Where(f => f.Name.ToLower().EndsWith(".ism", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                                OnDemandOrigin[0] = locator.Path + ismfile.Name + "/manifest";
                                OnDemandOrigin[1] = locator.Path + ismfile.Name + "/Manifest(format=mpd-time-csf)";
                                OnDemandOrigin[2] = locator.Path + ismfile.Name + "/Manifest(format=m3u8-aapl)";
                                OnDemandOrigin[3] = locator.Path + ismfile.Name + "/Manifest(format=m3u8-aapl-v3)";
                                Locator[6] = Newtonsoft.Json.JsonConvert.SerializeObject(OnDemandOrigin) ;
                             
                            }

                            if (locator.Type == LocatorType.Sas)
                            {
                                List<string> sas = new List<string>();
                                var mp4Files = assetFilesALL.Where(f => f.Name.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)).ToList();
                                foreach (var assetfilemp4 in mp4Files)
                                {
                                    var mp4Uri = new UriBuilder(locator.Path);
                                    mp4Uri.Path += "/" + assetfilemp4.Name;
                                    sas.Add(mp4Uri.ToString());
                                }
                                Locator[7] = Newtonsoft.Json.JsonConvert.SerializeObject(sas);
                            }
                            Locators.Add(locator.Id, Locator);
                        }
                        AssetDetail.Add("Locators", Locators);
                    }
                    allInfo.Add(theAsset.Id, AssetDetail);
                }
            }
            return Newtonsoft.Json.JsonConvert.SerializeObject(allInfo,Newtonsoft.Json.Formatting.Indented);
        }
        public StringBuilder GetStatsTxt()
        {
            StringBuilder sb = new StringBuilder();

            if (SelectedAssets.Count > 0)
            {
                // Asset Stats
                foreach (IAsset theAsset in SelectedAssets)
                {
                    string MyAssetType = AssetInfo.GetAssetType(theAsset);
                    bool bfileinasset = (theAsset.AssetFiles.Count() == 0) ? false : true;
                    long size = -1;
                    if (bfileinasset)
                    {
                        size = 0;
                        foreach (IAssetFile file in theAsset.AssetFiles)
                        {
                            size += file.ContentFileSize;
                        }
                    }
                    sb.AppendLine("Asset Name        : " + theAsset.Name);
                    sb.AppendLine("Asset Type        : " + theAsset.AssetType);
                    sb.AppendLine("Asset Id          : " + theAsset.Id);
                    sb.AppendLine("Alternate ID      : " + theAsset.AlternateId);
                    if (size != -1) sb.AppendLine("Size              : " + FormatByteSize(size));
                    sb.AppendLine("State             : " + theAsset.State);
                    sb.AppendLine("Created           : " + theAsset.Created.ToLongDateString() + " " + theAsset.Created.ToLongTimeString());
                    sb.AppendLine("Last Modified     : " + theAsset.LastModified.ToLongDateString() + " " + theAsset.LastModified.ToLongTimeString());
                    sb.AppendLine("Creations Options : " + theAsset.Options);

                    if (theAsset.State != AssetState.Deleted)
                    {
                        sb.AppendLine("IsStreamable      : " + theAsset.IsStreamable);
                        sb.AppendLine("SupportsDynEnc    : " + theAsset.SupportsDynamicEncryption);
                        sb.AppendLine("Uri               : " + theAsset.Uri.ToString());
                        sb.AppendLine("");
                        sb.AppendLine("Storage Name      : " + theAsset.StorageAccountName);
                        sb.AppendLine("Storage Bytes used: " + FormatByteSize(theAsset.StorageAccount.BytesUsed));
                        sb.AppendLine("Storage IsDefault : " + theAsset.StorageAccount.IsDefault);
                        sb.AppendLine("");

                        foreach (IAsset p_asset in theAsset.ParentAssets)
                        {
                            sb.AppendLine("Parent asset Name : " + p_asset.Name);
                            sb.AppendLine("Parent asset Id   : " + p_asset.Id);
                        }
                        sb.AppendLine("");
                        foreach (IContentKey key in theAsset.ContentKeys)
                        {
                            sb.AppendLine("Content key       : " + key.Name);
                            sb.AppendLine("Content key Id    : " + key.Id);
                            sb.AppendLine("Content key Type  : " + key.ContentKeyType);
                        }
                        sb.AppendLine("");
                        foreach (var pol in theAsset.DeliveryPolicies)
                        {
                            sb.AppendLine("Deliv policy Name : " + pol.Name);
                            sb.AppendLine("Deliv policy Id   : " + pol.Id);
                            sb.AppendLine("Deliv policy Type : " + pol.AssetDeliveryPolicyType);
                            sb.AppendLine("Deliv pol Protocol: " + pol.AssetDeliveryProtocol);
                        }
                        sb.AppendLine("");

                        foreach (IAssetFile fileItem in theAsset.AssetFiles)
                        {
                            if (fileItem.IsPrimary) sb.AppendLine("Primary");
                            sb.AppendLine("Name                 : " + fileItem.Name);
                            sb.AppendLine("Id                   : " + fileItem.Id);
                            sb.AppendLine("File size            : " + fileItem.ContentFileSize + " Bytes");
                            sb.AppendLine("Mime type            : " + fileItem.MimeType);
                            sb.AppendLine("Init vector          : " + fileItem.InitializationVector);
                            sb.AppendLine("Created              : " + fileItem.Created);
                            sb.AppendLine("Last modified        : " + fileItem.LastModified);
                            sb.AppendLine("Encrypted            : " + fileItem.IsEncrypted);
                            sb.AppendLine("EncryptionScheme     : " + fileItem.EncryptionScheme);
                            sb.AppendLine("EncryptionVersion    : " + fileItem.EncryptionVersion);
                            sb.AppendLine("Encryption key id    : " + fileItem.EncryptionKeyId);
                            sb.AppendLine("InitializationVector : " + fileItem.InitializationVector);
                            sb.AppendLine("ParentAssetId        : " + fileItem.ParentAssetId);

                            sb.AppendLine("==============");
                            sb.AppendLine("");
                        }

                        var assetFilesALL = theAsset.AssetFiles.ToList();

                        foreach (ILocator locator in theAsset.Locators)
                        {
                            sb.AppendLine("Locator Name      : " + locator.Name);
                            sb.AppendLine("Locator Type      : " + locator.Type.ToString());
                            sb.AppendLine("Locator Id        : " + locator.Id);
                            sb.AppendLine("Locator Path      : " + locator.Path);
                            if (locator.StartTime != null) sb.AppendLine("Start Time        : " + ((DateTime)locator.StartTime).ToLongDateString() + " " + ((DateTime)locator.StartTime).ToLongTimeString());
                            if (locator.ExpirationDateTime != null) sb.AppendLine("Expiration Time   : " + ((DateTime)locator.ExpirationDateTime).ToLongDateString() + " " + ((DateTime)locator.ExpirationDateTime).ToLongTimeString());
                            sb.AppendLine("");


                            if (locator.Type == LocatorType.OnDemandOrigin)
                            {

                                var ismfile = assetFilesALL.Where(f => f.Name.ToLower().EndsWith(".ism", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

                                sb.AppendLine(_prog_down_http + " : ");
                                foreach (IAssetFile IAF in theAsset.AssetFiles) sb.AppendLine(locator.Path + IAF.Name);
                                sb.AppendLine("");

                                sb.AppendLine(AssetInfo._smooth + " : ");
                                sb.AppendLine(locator.Path + ismfile.Name + "/manifest");

                                sb.AppendLine(AssetInfo._dash + " : ");
                                sb.AppendLine(locator.Path + ismfile.Name + "/Manifest(format=mpd-time-csf)");

                                sb.AppendLine(AssetInfo._hls_v4 + " : ");
                                sb.AppendLine(locator.Path + ismfile.Name + "/Manifest(format=m3u8-aapl)");

                                sb.AppendLine(AssetInfo._hls_v3 + " : ");
                                sb.AppendLine(locator.Path + ismfile.Name + "/Manifest(format=m3u8-aapl-v3)");
                            }
                            if (locator.Type == LocatorType.Sas)
                            {
                                sb.AppendLine(AssetInfo._prog_down_https + " : ");
                                var mp4Files = assetFilesALL.Where(f => f.Name.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)).ToList();
                                foreach (var assetfilemp4 in mp4Files)
                                {
                                    var mp4Uri = new UriBuilder(locator.Path);
                                    mp4Uri.Path += "/" + assetfilemp4.Name;
                                    sb.AppendLine(mp4Uri.ToString());
                                }
                            }
                            sb.AppendLine("");
                            sb.AppendLine("==============================================================================");
                            sb.AppendLine("");
                        }
                    }
                    sb.AppendLine("");
                    sb.AppendLine("+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
                    sb.AppendLine("");
                }
            }
            return sb;
        }

    }

}
