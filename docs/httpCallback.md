
HTML callback notification
==========================

Introduction
------------

To receive a HTTP Callback notification from Media Butler Framework
(MBF) you need to use httpNotificationStep step. This step do a HTTP GET
call with process instance id as parameter on the URL. The base URL is
reading from dot control file, so this step must be use in a multi file
package process.

First, Sample process definition
--------------------------------

To test the HTTP callback we could use this sample process:

1.  Ingest mezzanine file.

2.  Standard transcoding process using 1080p transcoding profile.

3.  Delete original asset.

4.  Create a streaming locator.

5.  Create a SAS locator.

6.  HTTP Callback to URL + process instance ID.

7.  Delete Original files from staging storage.

The json process definition on **ButlerConfiguration** table:

a.  PartitionKey: **MediaButler.Common.workflow.ProcessHandler**

b.  RowKey: **httpnotificationstep.ChainConfig**

c.  ConfigurationValue (json process definition):
```json
\[{

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

"TypeName": "MediaButler.BaseProcess.CreateSasLocatorStep",

"ConfigKey": ""

}, {

"AssemblyName": "MediaButler.BaseProcess.dll",

"TypeName": "MediaButler.BaseProcess.httpNotificationStep",

"ConfigKey": ""

}, {

"AssemblyName": "MediaButler.BaseProcess.dll",

"TypeName": "MediaButler.BaseProcess.DeleteOriginalBlobStep",

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
container with the name **httpnotificationstep**.

Third, Update container scan list
---------------------------------

At this moment you already have your stage container and process
definition for HTTP callback sample process. Now you need to update on
**ButlerConfiguration** table, the container list register. It is the
register with PartitionKey **MediaButler.Workflow.WorkerRole** and
RowKey **ContainersToScan**. You need to add to the list container name
**httpnotificationstep**. For example, the value could be:

testbasicprocess,httpnotificationstep

Fourth, Generate a dot control file
-----------------------------------

To use HTTP callback, you need to use multi file way with a dot control
information on json format. This file has the URL to call when the
process finish. Httpnotificationstep step will call this URL and append
process instance ID.

So a URL example could be

https://\[myAzureFunction\].azurewebsites.net/api/HttpTriggerCSharp1?code=mysecretCodeXXXXXXXXXXX==&processInstanceId=

This URL has incorporate on the URL the parameter processInstanceId, son
on runtime when MBF add the process instance ID you will be available to
read using this parameter.

Last, testing
-------------

Now you are available to submit a new file to transcoding, in a multi
file way. To do that you need to

1.  Generate a process instance id: it is a GUID number, for example:

    d6314efd-276a-4162-8227-9ddca0c01ee5

2.  Upload a sample MP4 to

    https://\[yourStorageAccount\].blob.core.windows.net/httpnotificationstep/
    **d6314efd-276a-4162-8227-9ddca0c01ee5/mySample.MP4**

3.  Upload dot control file with URL callback information

    The dot control file format is
```json
    {
      "baseUrl":"[youURLHere]"
    }
    
```

  The Upload must be to
  https://\[yourStorageAccount\].blob.core.windows.net/httpnotificationstep/**d6314efd-276a-4162-8227-9ddca0c01ee5/myDotControl.control**

This las step (\#3) trigger the process, after the process finish you
will receive the HTTP callback. To test this feature I am using Azure
function, so I am logging the HTTP call, and you can check the process
instance ID as a sample.

>2016-09-07T21:51:42 Welcome, you are now connected to log-streaming service.

>2016-09-07T21:51:45.523 Function started (Id=86dcbdce-9102-43d8-a186-b496c62503e0)

>2016-09-07T21:51:45.523 C\# HTTP trigger function processed a request.

>RequestUri=https://\[myAzureFunction\].azurewebsites.net/api/HttpTriggerCSharp1?code=XXXXXXXXXXXXXXXXXX&processInstanceId=**d6314efd-276a-4162-8227-9ddca0c01ee5**

>2016-09-07T21:51:45.523 Function completed (Success,Id=86dcbdce-9102-43d8-a186-b496c62503e0)

Update: you should restart Web Role after add a new process.
