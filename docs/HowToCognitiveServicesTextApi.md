
# How to use Cognitive Service Text API phrases extraction
## Introduction
Key phrase extraction, The API returns a list of strings denoting the key talking points in the input text. It employs techniques from Microsoft Office's sophisticated Natural Language Processing toolkit. English, German, Spanish, and Japanese text are supported.

The idea of this process is take a AMS asset with a VTT file inside and use this data to extract key phrases using the API and store the result inside the original AMS Asset

## First, Process definition
The process flow is:

1. Start
2. Select Asset by Name
3. Call Cognitive Services Text Analysis API
4. Insert result json doc on the Asset
5. Finish

The json process definition on **ButlerConfiguration** table:

a.  PartitionKey: **MediaButler.Common.workflow.ProcessHandler**

b.  RowKey: **textanalitycs.ChainConfig**

c.  ConfigurationValue (json process definition):

```json
[{
  "AssemblyName": "MediaButler.BaseProcess.dll",
  "TypeName": "MediaButler.BaseProcess.MessageHiddeControlStep",
  "ConfigKey": ""
 },

 {
  "AssemblyName": "MediaButler.BaseProcess.dll",
  "TypeName": "MediaButler.BaseProcess.SelectAssetByStep",
  "ConfigKey": ""
 }, {
  "AssemblyName": "MediaButler.BaseProcess.dll",
  "TypeName": "MediaButler.BaseProcess.CognitiveServices.TextAnalitycsStep",
  "ConfigKey": ""
 },

 {
  "AssemblyName": "MediaButler.BaseProcess.dll",
  "TypeName": "MediaButler.BaseProcess.DeleteOriginalBlobStep",
  "ConfigKey": ""
 }, {
  "AssemblyName": "MediaButler.BaseProcess.dll",
  "TypeName": "MediaButler.BaseProcess.MessageHiddeControlStep",
  "ConfigKey": ""
 }
]
```
## Second, create a stage container
MBF implement watch folder pattern on blob storage, each process has his own staging container. For this new process we need to create a container with the name **textanalitycs**.

##Third, Update container scan list
At this moment you already have your stage container and process definition for cognitive services text analitycs sample process. Now you need to update on **ButlerConfiguration** table, the container list register. It is the register with PartitionKey   **MediaButler.Workflow.WorkerRole** and RowKey **ContainersToScan**. You need to add to the list container name
**textanalitycs**. For example, the value could be:

testbasicprocess,textanalitycs

After update the container scan list, you always need to restar the Web Job.

##Fourth, Generate a dot control file

To use this sample process, you need to use multi file way with a dot control information on json format. 
On control file you define the asset name to use, languages of the video and the API URL and Key.
```json
{
  "SelectAssetBy.Type": "assetname",
  "SelectAssetBy.Value": "IntegrativeMomandWindowsPhoneAppStudio_high.mp4 - Indexed",
  "TextAnalitycsStep.Language": "en",
  "TextAnalitycsStep.apiURL": "https://westus.api.cognitive.microsoft.com/text/analytics/v2.0/keyPhrases",
  "TextAnalitycsStep.apiKey":  "[your key here]" 
}
```

Last, testing
-------------

Now you are available to submit a new file to transcoding, in a multi file way. To do that you need to

1.  Generate a process instance id: it is a GUID number, for example:

    d6314efd-276a-4162-8227-9ddca0c01ee5

2.  Upload dot control file on the container with the name /Incoming/d6314efd-276a-4162-8227-9ddca0c01ee5/youdoc.control

This last step (\#2) trigger the process, after the process.
