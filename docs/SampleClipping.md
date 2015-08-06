
<h1>Media Butler Sample Clipping</h1>
<h2>&nbsp;Introduction</h2>
&nbsp;Media Butler implement Clipping using dynamic filter, it is mean you can define video filter definition base on time. For example if you want to see the video for 0 to 10 second you can define a filter for that and use in the video URL as a parameter to apply the filter to the video. For example, if you defined a filter name <strong>winepart2</strong> in a <strong>myvideo</strong> asset, you can use this URL to access to the filter asset.
<p>
http://mediabutlerdev.streaming.mediaservices.windows.net/0e5e1064-18ac-450f-a392-f08534018892/myVideo.ism/Manifest(filtername=winepart2)
</p>
To use dynamic filter you need to have a multi bitrate asset. You can do a single VOD process to encode and create the filters or you can create/update the filter in an existent asset. In this document we will use a existent asset for simplicity of the sample.<p>
    
The process to implement  is 3 steps workflow.
    </p>
<ol>
    <li>&nbsp;Select the asset by name: using the asset name, you select the asset to create/ update the filter list (you can create one or more filters per asset)
</li>
    <li>Create or update the filter list</li>
    <li>Send notification back to blob storage </li>
</ol>
<h2>Create a new process</h2>
&nbsp;For setup a new process on Butler you need  to do:
 
<ol>
    <li>Create a new Staging blob storage 
</li>
    <li>Update Process configuration  and scan container list
</li>
    <li>Reboot Watcher role.</li>
    <li>testing </li>
</OL>

The configuration step for this process are:
<OL> 
    <li>&nbsp;Create a Staging Container in the blob storage. EX: <strong>testclipping</strong> </li>
    <li>&nbsp;Insert a configuration register in <strong>ButlerConfiguration</strong> Table Storage with this data
        <ol>
            <li>&nbsp;PartitioKey:       MediaButler.Common.workflow.ProcessHandler</li>
            <li>RowKey:            <strong>testclipping</strong>.ChainConfig</li>
            <li>ConfigurationValue: [json Process configuration sample in this document]

</li>
        </ol>
    </li>
    <li>&nbsp;Update ContainersToScan register in <strong>ButlerConfiguration</strong> storage Table, adding the new container. EX: 
        <ol>
            <li>PrimaryKey:      MediaButler.Workflow.WorkerRole</li>
            <li>RowKey:            ContainersToScan</li>
            <li>ConfigurationValue:     testbasicprocess, testclipping
 
</li>
        </ol>
    </li>
    <li>Reboot <strong>MediaButler.Watcher</strong> instance, to take the new container to scan configuration </li>
</ol>

<h3>    Process configuration sample</h3>
<pre>
[
    {
        "AssemblyName": "MediaButler.BaseProcess.dll",
        "TypeName": "MediaButler.BaseProcess.MessageHiddeControlStep",
        "ConfigKey": ""
    },
    {
        "AssemblyName": "MediaButler.BaseProcess.dll",
        "TypeName": "MediaButler.BaseProcess.Clipping.SelectAssetByNameStep",
        "ConfigKey": ""
    },
    {
        "AssemblyName": "MediaButler.BaseProcess.dll",
        "TypeName": "<strong>MediaButler.BaseProcess.Clipping.CreateClipFilterStep</strong>",
        "ConfigKey": ""
    },
    {
        "AssemblyName": "MediaButler.BaseProcess.dll",
        "TypeName": "MediaButler.BaseProcess.SendMessageBackStep",
        "ConfigKey": ""
    },
    {
        "AssemblyName": "MediaButler.BaseProcess.dll",
        "TypeName": "MediaButler.BaseProcess.MessageHiddeControlStep",
        "ConfigKey": ""
    }
]

    </pre>
   
<h2><span lang="EN-US" style="mso-fareast-font-family:&quot;Times New Roman&quot;;
mso-ansi-language:EN-US">Step by step test<o:p></o:p></span></h2>
<p>
    <span lang="EN-US" style="font-size:11.0pt;font-family:
&quot;Calibri&quot;,sans-serif;mso-fareast-font-family:Calibri;mso-fareast-theme-font:
minor-latin;mso-bidi-font-family:&quot;Times New Roman&quot;;mso-ansi-language:EN-US;
mso-fareast-language:EN-US;mso-bidi-language:AR-SA">To Execute a clipping test you need to do :</span></p>
<p>
I.	Create an control file to add the  clip information to the process. Media Butler support to add context process instance information inside a control file. The control file is a json file with <strong>.control </strong>extension. The control information example is this. It will create/update 2 filters for the asset "Chile is good for you (Chile hace bien)360-mp4-H264_Adaptive_Bitrate_MP4_Set_720p-Output" (you must use the name of your asset)</p>
<pre>
    {
    "assetName": "Chile is good for you (Chile hace bien)360-mp4-H264_Adaptive_Bitrate_MP4_Set_720p-Output",
    "assetId": "",
    "filterList": [
        {
            "filterName": "winepart",
            "ge": "60000000",
            "le": "200000000"
        },
        {
            "filterName": "winepart2",
            "ge": "200000000",
            "le": "250000000"
        }
    ]
}
    </pre>

<p>
    II.	Create an empty file <strong>feak.mp4</strong>, it is for legacy reason with old butler version.
    
</p>
<p>
    III.	Create a Blob folder container Incoming and GUID, for example <strong>Incoming/ 4EC1AD7A-91BF-4121-9EDF-5FE6211646B3</strong></p>
<p>
&nbsp;IV.	Upload feak.mp4 to the folder <span class="auto-style1"><strong>first</strong></span>
    
</p>
<p>
    V.	Upload control file.
    
</p>
<p>
    VI.	Wait to finish the process looking in <strong>Completed/ 4EC1AD7A-91BF-4121-9EDF-5FE6211646B3</strong>
    
</p>
<p>
    VII.	Open the file LOG feak.mp4.4-2-2015 12-57-39 PM.log and copy the Smooth Streaming URI for example:<br />
    <a href="http://mediabutlerdev.streaming.mediaservices.windows.net/a2d50a9e-262f-4513-8234-cbdd9113c541/ChileIsGoodForYou360.ism/manifest">http://mediabutlerdev.streaming.mediaservices.windows.net/a2d50a9e-262f-4513-8234-cbdd9113c541/ChileIsGoodForYou360.ism/manifest</a> </p>
<p>
    VIII.	Load the video in your player
IX.	Edit the URL for use the filter, for example<br />
    <a href="http://mediabutlerdev.streaming.mediaservices.windows.net/a2d50a9e-262f-4513-8234-cbdd9113c541/ChileIsGoodForYou360.ism/manifest(filtername=winepart">http://mediabutlerdev.streaming.mediaservices.windows.net/a2d50a9e-262f-4513-8234-cbdd9113c541/ChileIsGoodForYou360.ism/manifest(filtername=winepart</a>)
</p>
<p>
    X.	Edit the URL for use the filter, for example <br />
    <a href="http://mediabutlerdev.streaming.mediaservices.windows.net/a2d50a9e-262f-4513-8234-cbdd9113c541/ChileIsGoodForYou360.ism/manifest(filtername=winepart2">http://mediabutlerdev.streaming.mediaservices.windows.net/a2d50a9e-262f-4513-8234-cbdd9113c541/ChileIsGoodForYou360.ism/manifest(filtername=winepart) </p>

<h2>
    Related content</h2>
<ul>
    <li><a href="../README.md">Media Butler Framework repository</a></li>
    <li><a href="HowToDeploy.md">How to Deploy</a></li>
</ul>

