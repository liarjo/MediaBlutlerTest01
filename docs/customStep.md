
<html xmlns="http://www.w3.org/1999/xhtml">
<head>
  <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
  <meta http-equiv="Content-Style-Type" content="text/css" />
  <meta name="generator" content="pandoc" />
  <title></title>

</head>
<body>
<h2 id="how-to-extend-custom-step">How to extend custom step</h2>
<p>Media Butler Framework (MBF) support create new media workflow using all steps already included on the framework or by create new custom step using code. This tutorial explains how to create a new step and include it on the media workflow.</p>
<h2 id="create-a-new-step">1. Create a new step</h2>
<p>To create a new step you may create a new Class Library project as appear on the image.</p>
<p><img src="https://github.com/liarjo/MediaBlutlerTest01/blob/master/docs/howto_customStep1.JPG" width="624" height="433" /></p>
<p>Next, you need to add a reference to <strong>MediaButler.WorkflowStep.dll</strong> to have access to required interfaces. The interface to implement is <strong>ICustomStepExecution</strong>.</p>
<p><img src="https://github.com/liarjo/MediaBlutlerTest01/blob/master/docs/howto_customStep2.JPG" width="624" height="433" /></p>
<p><strong>ICustomStepExecution</strong> interface defines execute method, it is step’s custom logic container.</p>
<p>This sample code writes a message on the process instance metadata.</p>
<p>public class sampleCustomStep: MediaButler.WorkflowStep.ICustomStepExecution</p>
<p>{</p>
<p>public bool execute(ICustomRequest request)</p>
<p>{</p>
<p>string msg = string.Format(&quot;Hello Word! this is a custom step on processInstanceId {0} of processID {1}&quot;,</p>
<p>request.ProcessInstanceId,request.ProcessTypeId );</p>
<p>request.MetaData.Add(&quot;sampleCustomStep&quot;, msg);</p>
<p>return true;</p>
<p>}</p>
<p>}</p>
<p>With this simple implementation you already created a custom step.</p>
<h2 id="add-custom-step-to-media-workflow">2. Add custom step to media workflow</h2>
<p>After create a custom step you will need to add it on the media workflow. As any other media workflow on MBF you need to define the process on the <strong>ButlerConfiguration</strong> table.</p>
<p>To include the custom step you need to add a step implemented by <strong>MediaButler.BaseProcess.Control.MediaButlerCustomStep</strong>. This step is the bridge between the standard MBF step with a custom step.</p>
<p>The simplest process with only your custom step and delete the mezzanine file is thise:</p>
<p>[{</p>
<p>&quot;AssemblyName&quot;: &quot;MediaButler.BaseProcess.dll&quot;,</p>
<p>&quot;TypeName&quot;: &quot;MediaButler.BaseProcess.MessageHiddeControlStep&quot;,</p>
<p>&quot;ConfigKey&quot;: &quot;&quot;</p>
<p>}, {</p>
<p>&quot;AssemblyName&quot;: &quot;MediaButler.BaseProcess.dll&quot;,</p>
<p>&quot;TypeName&quot;: &quot;MediaButler.BaseProcess.Control.MediaButlerCustomStep&quot;,</p>
<p>&quot;ConfigKey&quot;: &quot;custome1&quot;</p>
<p>}, {</p>
<p>&quot;AssemblyName&quot;: &quot;MediaButler.BaseProcess.dll&quot;,</p>
<p>&quot;TypeName&quot;: &quot;MediaButler.BaseProcess.DeleteOriginalBlobStep&quot;,</p>
<p>&quot;ConfigKey&quot;: &quot;&quot;</p>
<p>}, {</p>
<p>&quot;AssemblyName&quot;: &quot;MediaButler.BaseProcess.dll&quot;,</p>
<p>&quot;TypeName&quot;: &quot;MediaButler.BaseProcess.MessageHiddeControlStep&quot;,</p>
<p>&quot;ConfigKey&quot;: &quot;&quot;</p>
<p>}]</p>
<p>MediaButlerCustomStep always need to have a configuration key because it is need information about your custom step. The configuration is like this</p>
<p>{</p>
<p>&quot;AssemblyName&quot;: &quot;custome1.dll&quot;,</p>
<p>&quot;TypeName&quot;: &quot;custome1.test1&quot;</p>
<p>}</p>
<p><strong>Assemblyname</strong> is the DLL path and name, on production the DLL will be on the same folder of the host for this reason you don’t need to add the path. On developer environment you can add full path to your DLL.</p>
<p><strong>TypeName</strong> is the class name who implement your custom step.</p>
<h2 id="test-the-custom-step">3. Test the custom step</h2>
<p>After you add the configuration on <strong>ButlerConfiguration</strong> table (process definition and MediaButlerCustomStep configuration) you are ready to copy a new video on the watch folder to trigger the process.</p>
<p>After the process finish, you can see on the ButlerWorkflowStatus table the process finish successfully (code -100) and can see on the Metadata element of the jsonContext the message wrote on your custom step:</p>
<p>&quot;sampleCustomStep&quot;: &quot;Hello Word! this is a custom step on processInstanceId e82881a2-df91-4824-83e2-e0caa85a847e of processID custome1&quot;</p>
</body>
</html>
