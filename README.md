
<b>Media Butler framework </b> is a VOD workflow automation framework for Azure Media Services. It support create different workflow using configuration, combining pre-defined steps or using customs steps create by code.
The basic workflow implementation is a folder watch folder but you can automate more complex scenarios like AMS replication cross regions.

Media butler is composed by 2 workers roles: Watcher and Workflow role. First one take the new files and submit it to Workflow Manager  by ButlerSend Queue. When a new job is summited, this role move the original files form Incoming folder to Processing.   Once the process finish, success or fail, this role receive a message and process it. If the process was success, it will move the original date from Processing to Success folder. In the fail case, will move to Fail folder.

Workflow Manager is Media Butler's core, it is the workflow coordinator. It receives jobs from ButlerSend queue, and process it following the process definition in ButlerConfiguration table. This role, follow and control the process and execute each step. When the process finish, it sends the notification as is configured. 

<img src="./docs/ButlerReadmeImg.JPG">

This vrstion has this process steps ready to use:

1.	Select AMS account: you can select in each process which  AMS account you want to use.
2.	Ingest mezzanine files:  one or more mezzamine files, MP4 and json files with parameters for the process.
3.	Standard encoder: using AMS encoder with preset or custom xml encoding profiles
4.	Package: existing MP4 for streaming, you don’t need to re-encode if you have ready your M4P files.
5.	Index: indexing video asset, it generates  Closed caption file in SAMI format,  Closed caption file in Timed Text Markup Language (TTML) format,  Keyword file (XML),  Audio indexing blob file (AIB) for use with SQL server
6.	Clipping: Create/update  ISMF files  for clipping
7.	Delete original mezzanine files
8.	Create streaming Locator
9.	Create SaS locator
10.	Queue Notification
11.	Mail Notification
12.	Blob text Notification
13.	Replica: replicate videos from one AMS to another AMS for HA deployments
