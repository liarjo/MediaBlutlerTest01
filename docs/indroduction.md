
<html xmlns="http://www.w3.org/1999/xhtml">
<head>

</head>
<body>
<h1 id="introduction">Media Butler Framework introduction</h1>
<p>Media Butler framework is a VOD workflow automation framework for Azure Media Services. It supports create different workflow using configuration, combining pre-defined steps or using customs steps create by code. The basic workflow implementation is a folder watch folder but you can automate more complex scenarios like AMS replication cross regions.</p>
<p>Media butler is composed by 2 workers process: Watcher and Workflow role. First one implements “watch folder” pattern on Azure Blob Storage. It takes the new files and submit it to Workflow Manager by ButlerSend Queue. When a new job is summited, this process moves the original files form Incoming folder to Processing. Once the process finish, success or fail, this process receives a message and process it. If the process was success, it will move the original date from Processing to Success folder. In the fail case, will move to Fail folder.</p>
<p>Workflow Manager is Media Butler's core, it is the workflow coordinator. It receives jobs from ButlerSend queue, and process it following the process definition in ButlerConfiguration table. This role, follow and control the process and execute each step. When the process finish, it sends the notification as is configured.</p>
<p><img src="https://github.com/liarjo/MediaBlutlerTest01/blob/master/docs/ButlerReadmeImg.JPG" width="624" height="384" /></p>
<h2 id="how-to-deploy-media-butler-framework">How to deploy Media Butler Framework</h2>
<h3 id="setup-pre-requisites">Setup pre requisites</h3>
<ol style="list-style-type: decimal">
<li><p>Azure Subscription</p></li>
<li><p>Azure Media Services Name and Key</p></li>
<li><p>Azure Media Services Storage Account Name and Key</p></li>
</ol>
<h3 id="deploy-media-butler-on-a-web-job">Deploy Media Butler on a Web JOB</h3>
<p>Media Butler Framework (MBF) has a deployment PowerShell <a href="https://github.com/liarjo/MediaBlutlerTest01/blob/master/Deployment/zeroTouchDeploy.ps1">script</a>. This script deploys MBF in Azure Web Job host always running. The script will create:</p>
<ol style="list-style-type: lower-alpha">
<li><p>MBF resource group</p></li>
<li><p>Staging storage account</p></li>
<li><p>Azure App Service Plan, App Service and Web Job</p></li>
</ol>
<p>As part of the deployment, this script will create a sample process. This sample process is for testing propose and has this steps:</p>
<ol style="list-style-type: decimal">
<li><p>Ingest the mezzanine file</p></li>
<li><p>Encode using default profile</p></li>
<li><p>Delete the original mezzanine asset</p></li>
<li><p>Create a Streaming locator</p></li>
<li><p>Create a SAS Locator</p></li>
<li><p>Write the process output info in the LOG file</p></li>
</ol>
<p>The scripts parameters are:</p>
<ol style="list-style-type: lower-alpha">
<li><p>MediaServiceAccountName: Media services account name.</p></li>
<li><p>MediaServiceAccountKey: Media services account Key.</p></li>
<li><p>MediaServiceStorageName: Media services storage account name.</p></li>
<li><p>MediaServiceStorageKey: Media Services account key.</p></li>
<li><p>SubscriptionName: Subscription name where deploy MBF.</p></li>
<li><p>MyClearTextUsername: (optional) user account name to use to execute the script.</p></li>
<li><p>MyClearTextPassword: (optional) user password.</p></li>
<li><p>appName: New resource manager name.</p></li>
<li><p>appRegion: Azure region, it must be the same of Azure Media Services.</p></li>
<li><p>overWriteRG: (true/false) overwrite if resource group exist.</p></li>
</ol>
<p>This script may be execute manually using user identity executing Login-AzureRmAccount command first. If you want to execute with other credential from Azure Active directory, you can set parameters MyClearTextUsername and MyClearTextPassword.</p>
<p>After execute the script you will receive this output, with the storage account name and key.</p>
<p>Stage Storaga Account Name mbfstagedeploytest81</p>
<p>Stage Storage Account Key XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXx==</p>
<p>WebSite Plan Name mbfwebjobhostdeploytest81</p>
<p>The resource group will see as this</p>
<p><img src="https://github.com/liarjo/MediaBlutlerTest01/blob/master/docs/ResourceGroupComponents.JPG" width="624" height="306" /></p>
<p>This is the stage storage account and you will use to upload the new videos there.</p>
<h3 id="test-the-deployment-process">Test the deployment process</h3>
<p>To test this deployment, you can follow this step by step:</p>
<ol style="list-style-type: decimal">
<li><p>Upload MP4 for “testbasicprocess” container in “Incoming” folder.</p></li>
<li><p>Check in Media Services content a new asset with pattern name “testbasicprocess_[your MP4 video Name]Butler[GUID]”</p></li>
<li><p>Check in Media Services JOB list a new job</p></li>
<li><p>When the job finish, check the final asset Encoded and published with the patter name “testbasicprocess_[your MP4 video Name]__Butler_[GUID]_mb”</p></li>
<li><p>Now, you could go to the Media Butler Storage, and review the output info in the file testbasicprocess/Completed/[your MP4 video Name].[date and time].log</p></li>
</ol>
<h3 id="more-information">More information</h3>

<ol style="list-style-type: decimal">
<li><p><a href="https://github.com/liarjo/MediaBlutlerTest01/blob/master/docs/DolbySample.md">Automation of Delivering Premium Audio Experiences with Dolby Digital Plus in Azure</a></p></li>
<li><p><a href="https://github.com/liarjo/MediaBlutlerTest01/blob/master/docs/PremiunEncoder.md">Automation of Premium Encoding in Azure Media Services with Media Butler</a></p></li>
<li><p><a href="https://github.com/liarjo/MediaBlutlerTest01/blob/master/docs/SampleClipping.md">Media Butler Sample Clipping</a></p></li>
<li><p><a href="https://github.com/liarjo/MediaBlutlerTest01/blob/master/docs/customStep.md">Media Butler How to extend with custom step</a></p></li>
<li><p><a href="https://github.com/liarjo/MediaBlutlerTest01/blob/master/docs/query.md">How To Query Media Butler Process Status</a></p></li>
<li><p><a href="https://github.com/liarjo/MediaBlutlerTest01/blob/master/docs/watchfolder.md">How to trigger Media Butler process using a specific process instance id</a></p></li>

</ol>


</body>
</html>
