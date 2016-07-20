
<html xmlns="http://www.w3.org/1999/xhtml">
<head>

</head>
<body>
<h2 id="how-to-trigger-a-process-using-a-specific-process-instance-id">How to trigger a process using a specific process instance id</h2>
<h3 id="watch-folder">Watch folder</h3>
<p>Media Butler Framework (MBF) implement watch folder patter, so the way to trigger a new process instance is dropping a file in the watch folder.</p>
<p>For example, using the sample process the watch folder is</p>
<p>https://[my storage account].blob.core.windows.net/<strong>testbasicprocess/Incoming</strong></p>
<p>Now, this watch folder is for single file and auto generate process instance id. That means you only can ingest to the process a single video file and the process instance Id will be generate by MBF internally. This kind of watch folder is good for simple process that only needs on video file.</p>
<p>Now, if you need to control the process instance id or you need to ingest more than a single file you should use advance watch folder.</p>
<h3 id="advance-watch-folder">Advance watch folder</h3>
<p>The only differences between watch folder and advance watch folder are:</p>
<ol style="list-style-type: decimal">
<li><p>The path.</p>
<p>You need to use a sub folder with a guid name, this guid is the process instance id.</p>
<p>Example:</p>
<p>https://[my storage account].blob.core.windows.net/<strong>testbasicprocess/Incoming/[process instanceid]</strong></p></li>
<li><p>Control file</p>
<p>Because advance watch folder support multiples files you need to send a traffic light control to trigger the process. This traffic light file is any file dot control. Example: blank.control.</p>
<p>In advanced scenarios control file could contain process instance’s metadata and configuration in json format.</p></li>
</ol>
<h2 id="step-by-step">Step by Step </h2>
<p>To trigger sample process with specific process instance id you need to follow this steps:</p>
<ol style="list-style-type: decimal">
<li><p>Generate process instance id, for example a6c3bde9-4456-4747-b188-301b2bc5cd5a</p></li>
<li><p>Upload video file to the advance watch folder, for example  https://xxxxxxxx.blob.core.windows.net/testbasicprocess/Incoming/a6c3bde9-4456-4747-b188-301b2bc5cd5a/</p></li>
<li><p>Upload an empty file with name blank.control to the same folder.</p></li>
</ol>
<p>After that, because the watch folder process found control file trigger the process.</p>
</body>
</html>
