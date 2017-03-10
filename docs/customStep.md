# How to extend VOD workflow with a Custom Step
## Introduction
Media Butler Framework (MBF) support create new media workflow using all steps already included on the framework or by create new custom step using C# code. This tutorial explains how to create a new custom step and include it on the media workflow by configuration.

## 1. Create a new Custom Step with VS
To create a new step you may create a new Class Library project as appear on the image.

![alt text](https://github.com/liarjo/MediaBlutlerTest01/raw/master/docs/howto_customStep1.JPG "Create new project")

Next, you need to add a reference to **MediaButler.WorkflowStep.dll** to have access to required interfaces. The interface to implement is **ICustomStepExecution**.

![alt text](https://github.com/liarjo/MediaBlutlerTest01/raw/master/docs/howto_customStep2.JPG "Add project reference")

**ICustomStepExecution** interface defines execute method, it is step’s custom logic container.
This sample code writes a message on the process instance metadata.

```csharp
public class sampleCustomStep : MediaButler.WorkflowStep.ICustomStepExecution
    {
        public bool execute(ICustomRequest request)
        {
            string msg = string.Format(
                "Hello Word! this is a custom step on processInstanceId {0} of processID {1}",
                request.ProcessInstanceId,
                request.ProcessTypeId);
            request.MetaData.Add("sampleCustomStep", msg);
            return true;
        }
    }
```
With this simple implementation you already created a custom step.

## 2. Add custom step to media workflow
After create a custom step you will need to add it on the media workflow. As any other media workflow on MBF you need to define the process on the **ButlerConfiguration** table and add Custom Step’s configuration. 
Also you need to copy your DLL to MBS's storage on **mediabutlerbin** container.

### 2.1 Add Custom Step to MBF workflow
To include the custom step you need to add a step implemented by **MediaButler.BaseProcess.Control.MediaButlerCustomStep**. This step is the bridge between the standard MBF step with a custom step.
This process example deffintion process only ingest your mezzanine file, execute  your custom step and delete the mezzanine.

```json
[{

    "AssemblyName": "MediaButler.BaseProcess.dll",

    "TypeName": "MediaButler.BaseProcess.MessageHiddeControlStep",

    "ConfigKey": ""

}, {

    "AssemblyName": "MediaButler.BaseProcess.dll",

    "TypeName": "MediaButler.BaseProcess.Control.MediaButlerCustomStep",

    "ConfigKey": "custome1"

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

### 2.2 Add Custom Step's confiuration
Your Custom Step is a  exteranl DLL so you need to reference it  in a new entity on  **ButlerConfiguration** table. The partition Key for this new entity is ** MediaButler.Common.workflow.ProcessHandler** and the row key **[your row key name].StepConfig**.
On the process example value of **[ your row key name]** is ** custome1** so the row key for the example is shoult be **custome1.StepConfig**.
Now the  **ConfigurationValue** for this sample custom step is

```json
{
 "AssemblyName": "HelloWordStep.dll",
 "TypeName": "HelloWordStep.sampleCustomStep"
}
```

**Assemblyname** is the DLL path and name, on production the DLL will be on the same folder of the host for this reason you don’t need to add the path. On developer environment you can add full path to your DLL.

**TypeName** is the class name who implement your custom step.
### 2.1 Custom Step DLL deployment
Last configuration task is upoload new DLL to MBF storage on **mediabutlerbin** and restart the web job.

## 3. Test sample Custom Step
After you add the configuration on **ButlerConfiguration** table (process definition and MediaButlerCustomStep configuration) you are ready to copy a new video on the watch folder to trigger the process.

After the process finish, you can see on the **ButlerWorkflowStatus** table the process finish successfully (code -100) and can see on the Metadata element of the jsonContext the message wrote on your custom step:

"sampleCustomStep": "Hello Word! this is a custom step on processInstanceId e82881a2-df91-4824-83e2-e0caa85a847e of processID custome1"

Here you have a more advance MBF Custom Step sample to genearte a Http Callback notification on a custom step.

[MBF-CustomeStep-Sample](https://github.com/liarjo/MBF-CustomeStep-Sample "MBF-CustomeStep-Sample")

