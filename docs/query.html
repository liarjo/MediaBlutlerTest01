
<html xmlns="http://www.w3.org/1999/xhtml">
<head>

</head>
<body>
<h2 id="introduction">Introduction</h2>
<p>Any workflow manager needs to have a way to see an instance status or error messages to provide this information to the user or any other system.</p>
<p>All process Instance status are store on “ButlerWorkflowStatus” Azure Table with this schema:</p>
<ul>
<li><p>PartitionKey: it is the process Id.</p></li>
<li><p>RowKey: it is process instance id.</p></li>
<li><p>Timestamp</p></li>
<li><p>CurrentStep: process instance current step, the steps id starts in 0 to X where X is the last step of the workflow. When the process instance finish, the id could be:</p>
<ul>
<li><p>-100: Success</p></li>
<li><p>-200: Error and retrying</p></li>
<li><p>-300: process instance finish with error</p></li>
<li><p>-400: Process error configuration, not executed</p></li>
</ul></li>
<li><p>jsonContext: it is the json document with all process instance information.</p></li>
</ul>
<p>Media Butler Framework (MBF) gives the option to query this information directly from Azure Table Storage using native API. You can see Table Service REST API documentation <a href="https://msdn.microsoft.com/en-us/library/azure/dd179423.aspx">here</a>.</p>
<h2 id="how-to-query-status">How to query status </h2>
<p>The easiest but not the only way to read information from “ButlerWorkflowStatus” Azure Table is using Shared Access Signatures (SAS). Here you have an article to understand <a href="https://azure.microsoft.com/en-us/documentation/articles/storage-dotnet-shared-access-signature-part-1/">Shared Access Signatures</a>.</p>
<p>The idea is generated a SAS token with query access only to the table level. The URL to query looks like this example:</p>
<p>https://[myStoargeAccount].table.core.windows.net/ButlerWorkflowStatus?tn=ButlerWorkflowStatus&amp;[my SAS key]</p>
<p>To receive the response on json format you need to add Accept header with value application/json;odata=minimalmetadata.</p>
<p>Good documentations about querying Tables and entities is <a href="https://msdn.microsoft.com/en-us/library/dd894031.aspx">here</a>.</p>
<h3 id="how-to-read-all-instance-of-a-specific-process">How to read all instance of a specific process</h3>
<p>To read all instance of specific process you only need to use this query pattern</p>
<p>https://[myStoargeAccount].table.core.windows.net/ButlerWorkflowStatus?tn=ButlerWorkflowStatus&amp;[my SAS key]&amp;$filter=PartitionKey%20eq%20'[Process ID]'</p>
<p>for example for ‘testbasicprocess’ you can use</p>
<p>https://XXXXXXXXXXXXXXXX.table.core.windows.net/ButlerWorkflowStatus?tn=ButlerWorkflowStatus&amp;sv=2015-02-21&amp;st=2016-07-20T17%3A14%3A33Z&amp;se=2019-07-20T18%3A14%3A00Z&amp;sp=r&amp;sig= XXXXXXXXXXXXXXXX &amp;$filter=PartitionKey%20eq%20'testbasicprocess'</p>
<h3 id="how-to-read-specific-process-instance-information">How to read specific process instance information</h3>
<p>To read status of a specific process instance you need to keys, first process ID or process name and second process instance id. For example, using the sample process included on the deployment script the process Id is “testbasicprocess” and the process instance ID is a GUID number auto generate or assigned at the moment of drop files on the watch folder.</p>
<p>For example, process instance id 0ae0fdc8-81fa-4cda-9d4c-f2443bfd5c9d this is the query</p>
<p>https://XXXXXXXXXXXXXXXX.table.core.windows.net/ButlerWorkflowStatus?tn=ButlerWorkflowStatus&amp;sv=2015-02-21&amp;st=2016-07-20T17%3A14%3A33Z&amp;se=2019-07-20T18%3A14%3A00Z&amp;sp=r&amp;sig=XXXXXXXXXXXXXXXXXXXx&amp;$filter=PartitionKey%20eq%20'testbasicprocess'%20and%20RowKey%20eq%20'0ae0fdc8-81fa-4cda-9d4c-f2443bfd5c9d'</p>
<h2 id="mbf-jsoncontext-schema">MBF jsonContext schema</h2>
<p>MBF jsonContext contains all the instance information, this are the principal fields:</p>
<ol style="list-style-type: decimal">
<li><p>Exceptions: list of exceptions on the process instance execution</p></li>
<li><p>AssetId: Media Services AssetId of current asset if it applies.</p></li>
<li><p>ButlerRequest: All information about the request.</p></li>
<li><p>MediaAccountName: Media services account name</p></li>
<li><p>MediaAccountKey: Media Services account key</p></li>
<li><p>MediaStorageConn: Media services storage account connection string</p></li>
<li><p>TimeStampProcessingStarted: timestamp process instance start</p></li>
<li><p>Log: string log messages write on the process instance</p></li>
<li><p>MetaData: Key value string metadata generated internally on process instance</p></li>
<li><p>ProcessTypeId: process id</p></li>
<li><p>ProcessInstanceId: process instance id</p></li>
<li><p>ProcessConfigConn: process storage account connection string</p></li>
</ol>
<p>MBF jsonContext sample is <a href="jsonContextSample.json">here.</a></p>
</body>
</html>
