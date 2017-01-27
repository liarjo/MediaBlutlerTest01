
Add local filter to AMS Asset using XML
==========================

Introduction
------------
Starting with 2.11 release, Media Services enables you to define filters for your assets. These filters are server side rules that will allow your customers to choose to do things like: playback only a section of a video (instead of playing the whole video), or specify only a subset of audio and video renditions that your customer's device can handle (instead of all the renditions that are associated with the asset). This filtering of your assets is archived through **Dynamic Manifests** that are created upon your customer's request to stream a video based on specified filter(s).

more details about fiters [here](https://docs.microsoft.com/en-us/azure/media-services/media-services-dynamic-manifest-overview).

The idea of this post is show how to add a local filter base on a XML defintion in a VOD process of AMS.

First, Sample process definition
--------------------------------

To add filetr feature have to use a multi file trigger process:

1.  Ingest mezzanine files.

2.  Standard transcoding process using 1080p transcoding profile.

3.  Delete original asset.

4.  Create a streaming locator.

5.  Add local filter definition.

6.  Delete Original files from staging storage.

The json process definition on **ButlerConfiguration** table:

a.  PartitionKey: **MediaButler.Common.workflow.ProcessHandler**

b.  RowKey: **addassetfilterxml.ChainConfig**

c.  ConfigurationValue (json process definition):
```json
[{
 "AssemblyName": "MediaButler.BaseProcess.dll",
 "TypeName": "MediaButler.BaseProcess.MessageHiddeControlStep",
 "ConfigKey": ""
}, {
 "AssemblyName": "MediaButler.BaseProcess.dll",
 "TypeName": "MediaButler.BaseProcess.IngestMultiMezzamineFilesStep",
 "ConfigKey": ""
}, {
 "AssemblyName": "MediaButler.BaseProcess.dll",
 "TypeName": "MediaButler.BaseProcess.StandarEncodeStep",
 "ConfigKey": "StandarEncodeStep"
}, {
 "AssemblyName": "MediaButler.BaseProcess.dll",
 "TypeName": "MediaButler.BaseProcess.DeleteOriginalAssetStep",
 "ConfigKey": ""
}, {
 "AssemblyName": "MediaButler.BaseProcess.dll",
 "TypeName": "MediaButler.BaseProcess.CreateStreamingLocatorStep",
 "ConfigKey": ""
}, {
 "AssemblyName": "MediaButler.BaseProcess.dll",
 "TypeName": "MediaButler.BaseProcess.Clipping.AddAssetFilterXmlStep",
 "ConfigKey": ""
}, {
 "AssemblyName": "MediaButler.BaseProcess.dll",
 "TypeName": "MediaButler.BaseProcess.SendMessageBackStep",
 "ConfigKey": ""
}, {
 "AssemblyName": "MediaButler.BaseProcess.dll",
 "TypeName": "MediaButler.BaseProcess.MessageHiddeControlStep",
 "ConfigKey": ""
}]
```
Second, create a stage container
--------------------------------

MBF implement watch folder pattern on blob storage, each process has his
own staging container. For this new process we need to create a
container with the name **addassetfilterxml**.

Third, Update container scan list
---------------------------------

At this moment you already have your stage container and process
definition for HTTP callback sample process. Now you need to update on
**ButlerConfiguration** table, the container list register. It is the
register with PartitionKey **MediaButler.Workflow.WorkerRole** and
RowKey **ContainersToScan**. You need to add to the list container name
**addassetfilterxml**. For example, the value could be:

>>testbasicprocess,addassetfilterxml

After update the container scan list, you always need to **restar the Web Job**.

Fourth, XML file definition
-----------------------------------

To add filters to your AMS asset, you need to use multi file way with  adding a XML filter definition.
for example this definition has 4 filters on it.
```xml
<?xml version="1.0" encoding="utf-8"?>
<Filters majorVersion="1" minorVersion="1">
  <Filter name="filtro1">
    <Range start="100000000" end="200000000" />
  </Filter>
  <Filter name="programdvr">
    <Range prw="213408000000" />
  </Filter>
  <Filter name="livebackoff">
    <Range backoff="100000000" />
  </Filter>
  <Filter name="combinado">
    <Range start="169770000" end="322170000" prw="332640000000" backoff="100000000" />
  </Filter>
</Filters>
```
Now, becouse is a multi file trigger you need a dot control file. In this sample could be a empty file.


Last, testing
-------------

Now you are available to submit a new file to transcoding, in a multi
file way. To do that you need to

1.  Generate a process instance id: it is a GUID number, for example:
    d6314efd-276a-4162-8227-9ddca0c01ee5
2.  Upload a sample MP4 to
    https://\[yourStorageAccount\].blob.core.windows.net/**addassetfilterxml/
    d6314efd-276a-4162-8227-9ddca0c01ee5/mySample.MP4**
3.  Upload XML filter file with the name **_azuremediaservices.config**
4.  Upload empty dot control file
    The dot control file format is
```json
    {
    }
```

  The Upload must be to
  https://\[yourStorageAccount\].blob.core.windows.net/**addassetfilterxml/d6314efd-276a-4162-8227-9ddca0c01ee5/myDotControl.control**

This last step (\#4) trigger the process, after the new asset is finish you will see the filter information on the assett details.

![alt text](https://github.com/liarjo/MediaBlutlerTest01/blob/master/docs/filters.JPG "Filters!")

