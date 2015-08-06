<h1>Automation of Premium Encoding in Azure Media Services with Media Butler</h1>
<h2>Introduction</h2>
You can review AMS Premium Encoder documentation <a href="http://azure.microsoft.com/blog/2015/03/05/introducing-premium-encoding-in-azure-media-services/">here</a>
 and AMS Explorer to submit premium encoder task <a href="http://azure.microsoft.com/blog/2015/03/06/how-to-use-premium-encoding-in-azure-media-services/">here</a>.
<p>
Pre-requisite: You have to have Azure Media Butler already deployed to use this capability.  <a href="https://github.com/liarjo/MediaBlutlerTest01/blob/master/README.md">Here</a> is the link on how to deploy the media butler framework.

</p>
<p>
This document describes how to implement a VOD automation process with Azure Media Butler to encode videos using Azure Media Services premium Encoder. The process is triggered based on a watch folder pattern. If you upload a video and a workflow encode definition file, Media Butler will encoded it and send a notification.</p>
<p>The process is shown in the following diagram.</p>
<p><img src="./preminencoderprocess1.jpg"</p>
<p>Azure Media Butler VOD process has 5 steps:
<ol>
	<li><b>Ingest Mezzanine File(s)</b>: Read all file(s) from the staging container and ingest in a new Asset. </li>
	<li><b>Encode:</b> Encode the video asset using the premium workflow definition.</li>
	<li><b>Delete the original Asset:</b> delete mezzanine file(s) and Workflow file asset(s).</li>
	<li><b>Process end notification:</b> send the process output log to Complete Blob Container (Complete BLOB container is a storage location for output files).</li>
</ol>
</p>
<p>
Azure Media Butler Framework can process multiple files in parallel, you only need to define the process in the configuration table.  Now we will setup the process illustrated in the previous diagram by adding a configuration file in json format.
</p>
<h2>Create a new process</h2>
<p>For setup a new process on Butler you need to create a staging blob container and add the process configuration. Here you have step by step sample.</p>
<ol>
<li><b>Create a new staging blob storage container.</b><br>
Example:<i> <b>testpremiunencoder</b> blob container.</i> </li>
<li><b>Add the process configuration.</b><br>
Insert a configuration record in <b>ButlerConfiguration</B> Table Storage with this data<br>
Example:<br>
	<i>
		<ol type="a">
			<li>PartitioKey: MediaButler.Common.workflow.ProcessHandler</li>
			<li>RowKey: <b>testpremiunencoder</B>.ChainConfig</li>
			<li>ConfigurationValue:
				<br>
				[
{"AssemblyName":"MediaButler.BaseProcess.dll","TypeName":"MediaButler.BaseProcess.MessageHiddeControlStep","ConfigKey":""},
{"AssemblyName":"MediaButler.BaseProcess.dll","TypeName":"MediaButler.BaseProcess.IngestMultiMezzamineFilesStep","ConfigKey":""},
<B>{"AssemblyName":"MediaButler.BaseProcess.dll","TypeName":"MediaButler.PremiunEncoder.PremiumEncodingStep","ConfigKey":""},</B>
{"AssemblyName":"MediaButler.BaseProcess.dll","TypeName":"MediaButler.BaseProcess.DeleteOriginalAssetStep","ConfigKey":""},
{"AssemblyName":"MediaButler.BaseProcess.dll","TypeName":"MediaButler.BaseProcess.CreateSasLocatorStep","ConfigKey":""},
{"AssemblyName":"MediaButler.BaseProcess.dll","TypeName":"MediaButler.BaseProcess.SendMessageBackStep","ConfigKey":""},
{"AssemblyName":"MediaButler.BaseProcess.dll","TypeName":"MediaButler.BaseProcess.MessageHiddeControlStep","ConfigKey":""}
]
			</li>
		</ol>
	</i>
</li>	
<li><B>Update ContainersToScan record in ButlerConfiguration storage Table</B>, adding the new container.<br>
				Example:
				<i>
					<ol type="a">
						<li>PrimaryKey: MediaButler.Workflow.WorkerRole</li>
						<li>RowKey: <b>ContainersToScan</B></li>
						<li>ConfigurationValue: testbasicprocess, <b>testpremiunencoder</B></li>
					</ol>		
				</i>
</li>
</ol>
<h2>Testing the process</h2>
To test the new process you need the following:
<ol>
	<li>A sample video (mezzanine video file) to upload. (i.e an MFX file)</li>
	<li>A workflow definition file that you have created using the desktop workflow designer which is explained <a href="http://azure.microsoft.com/blog/2015/03/05/introducing-premium-encoding-in-azure-media-services/"> here.</a></li>
	You must upload all files in the Media Butler “Incoming” blob folder.  Then you need to upload
	<li>A Media Butler control file (for triggering the process).  Control file is a json file with .control extension</li>

</ol>
	
Now follow these steps:
<ol>
	<li>Create a Blob folder container Incoming and GUID, for example <i><b>Incoming/4EC1AD7A-91BF-4121-9EDF-5FE6211646B3</b></i></li>
	<li>Upload the video Mezzanine file and a workflow definition file to blob storage container “incoming” </li>
	<li>Upload control file to “incoming”. (For example empty file <b>go.control</b> to trigger the process) </li>
	<li>Check the new Assets and jobs created in AMS</li>
	<li>Wait to finish the process looking in blob storage container <i><b>Completed/4EC1AD7A-91BF-4121-9EDF-5FE6211646B3</b></i></li>
	<li>Check in Media Services the output asset, it is published for progressive download.</li>
</ol>
<h2>Related content</h2>
<li><a href="https://github.com/liarjo/MediaBlutlerTest01/blob/master/README.md">Media Butler Framework repository</a></li>
<li><a href="https://github.com/liarjo/MediaBlutlerTest01/blob/master/docs/HowToDeploy.md">How to deploy</a></li>
