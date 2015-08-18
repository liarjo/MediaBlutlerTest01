<h1 id="automation-of-delivering-premium-audio-experiences-with-dolby-digital-plus-in-azure">Automation of Delivering Premium Audio Experiences with Dolby Digital Plus in Azure</h1>
<p>Dolby Digital Plus is an advanced surround sound audio technology that's built into home theaters, smartphones, operating systems, and browsers. More information <a href="http://www.dolby.com/us/en/technologies/dolby-digital-plus.html">here</a>.</p>
<p>Azure Media Services Encoder support use Dolby Plus encode for produce premium audio experience with your content. <a href="http://azure.microsoft.com/blog/2014/09/03/delivering-premium-audio-experiences-with-dolby-digital-plus/">Here</a> you have a detail post about how it work this encoder with Media Services.</p>
<p>Here we will do a Media Butler Framework configuration to automate the VOD process and use this encoder to transcode the audio. Butler does process automation, you can see more info <a href="http://aka.ms/mediabutlerframework">here</a>.</p>
<p>The process to configure is showed in the following diagram and the whole process uses Media Butler Framework standard steps. You do not need to do custom code to use this encoder, only write the right configuration.</p>
<p><img src="./workflow.jpg" alt="" /></p>
<h2 id="azure-media-butler-dolby-digital-plus-vod-process-has-6-steps">Azure Media Butler Dolby Digital Plus VOD process has 6 steps:</h2>
<ol style="list-style-type: decimal">
<li><p><strong>Ingest Mezzanine Files:</strong> Read all files from the staging container and ingest in a new Asset.</p></li>
<li><p><strong>Encode:</strong> Encode the video asset using the Media Services Encoder with custom Encoder Audio Preset.</p></li>
<li><p><strong>Delete the original Asset:</strong> delete mezzanine files assets.</p></li>
<li><p><strong>Publish SAS Locator:</strong> Locator provides an entry point to access the files contained in an Asset, here you create one<strong>.</strong></p></li>
<li><p><strong>Send Mail Notification:</strong> Using Send Grid services, send the output process information notification.</p></li>
<li><p><strong>Delete Original Blob Files</strong>: clean the Stage Blob container.</p></li>
</ol>
<h2 id="create-a-new-process">Create a new process</h2>
<p>To setup a new process on Butler you need to create a staging blob container and add the process configuration. Follow this step by step sample.</p>
<ol style="list-style-type: decimal">
<li><p>Create a new staging blob storage container.</p>
<p>Example: <strong>my<em>dolby</em></strong> <em>blob container</em>.</p></li>
<li><p><strong>Add the process configuration</strong>.</p>
<p>Insert a configuration record in <strong>ButlerConfiguration</strong> Table Storage with this data</p>
<p>Example:</p>
<ol style="list-style-type: lower-alpha">
<li><p><em><strong>PartitioKey</strong>: MediaButler.Common.workflow.ProcessHandler</em></p></li>
<li><p><em><strong>RowKey</strong>: <strong>mydolby</strong>.ChainConfig</em></p></li>
<li><p><em><strong>ConfigurationValue</strong>:</em></p></li>
</ol></li>
</ol>
<blockquote>
<p><em>[</em></p>
<p><em>{&quot;AssemblyName&quot;:&quot;MediaButler.BaseProcess.dll&quot;,&quot;TypeName&quot;:&quot;MediaButler.BaseProcess.MessageHiddeControlStep&quot;,&quot;ConfigKey&quot;:&quot;&quot;},</em></p>
<p><em>{&quot;AssemblyName&quot;:&quot;MediaButler.BaseProcess.dll&quot;,&quot;TypeName&quot;:&quot;MediaButler.BaseProcess.IngestMultiMezzamineFilesStep&quot;,&quot;ConfigKey&quot;:&quot;&quot;},</em></p>
<p><em>{&quot;AssemblyName&quot;:&quot;MediaButler.BaseProcess.dll&quot;,&quot;TypeName&quot;:&quot;MediaButler.BaseProcess.StandarEncodeStep&quot;,&quot;ConfigKey&quot;:&quot;<strong>DolbyAudioPreset</strong>&quot;},</em></p>
<p><em>{&quot;AssemblyName&quot;:&quot;MediaButler.BaseProcess.dll&quot;,&quot;TypeName&quot;:&quot;MediaButler.BaseProcess.DeleteOriginalAssetStep&quot;,&quot;ConfigKey&quot;:&quot;&quot;},</em></p>
<p><em>{&quot;AssemblyName&quot;:&quot;MediaButler.BaseProcess.dll&quot;,&quot;TypeName&quot;:&quot;MediaButler.BaseProcess.CreateSasLocatorStep&quot;,&quot;ConfigKey&quot;:&quot;&quot;},</em></p>
<p><em>{&quot;AssemblyName&quot;:&quot;MediaButler.BaseProcess.dll&quot;,&quot;TypeName&quot;:&quot;MediaButler.BaseProcess.SendGridStep&quot;,&quot;ConfigKey&quot;:&quot;<strong>DolbySendGridStep</strong>&quot;},</em></p>
<p><em>{&quot;AssemblyName&quot;:&quot;MediaButler.BaseProcess.dll&quot;,&quot;TypeName&quot;:&quot;MediaButler.BaseProcess.DeleteOriginalBlobStep&quot;,&quot;ConfigKey&quot;:&quot;&quot;},</em></p>
<p><em>{&quot;AssemblyName&quot;:&quot;MediaButler.BaseProcess.dll&quot;,&quot;TypeName&quot;:&quot;MediaButler.BaseProcess.MessageHiddeControlStep&quot;,&quot;ConfigKey&quot;:&quot;&quot;}</em></p>
<p><em>]</em></p>
</blockquote>
<ol style="list-style-type: decimal">
<li><p>Create XML encoder preset and upload to <strong>mediabulterbin</strong> container</p>
<p>The preset is a XML file, following the example use the name <strong>myDolbyPreset.xml</strong></p></li>
<li><p>Add <strong>StandarEncodeStep</strong> configuration</p>
<p>Insert a configuration record in <strong>ButlerConfiguration</strong> Table Storage with this data</p>
<p>Example:</p>
<p>a. <strong>PartitioKey</strong>: MediaButler.Common.workflow.ProcessHandler</p>
<p>b. <strong>RowKey</strong>: <em>DolbyAudioPreset.StepConfig</em></p>
<p>c. <strong>ConfigurationValue</strong>: <em><strong>myDolbyPreset.xml</strong></em></p>
<p>The content of <strong>myDolbyPreset.xml</strong> is</p>
<p>&lt;?xml version=&quot;1.0&quot; encoding=&quot;utf-16&quot;?&gt;</p>
<p>&lt;Presets&gt;</p>
<p>&lt;Preset</p>
<p>Version=&quot;5.0&quot;&gt;</p>
<p>&lt;MediaFile</p>
<p>DeinterlaceMode=&quot;AutoPixelAdaptive&quot;</p>
<p>ResizeQuality=&quot;Super&quot;</p>
<p>VideoResizeMode=&quot;Stretch&quot;&gt;</p>
<p>&lt;OutputFormat&gt;</p>
<p>&lt;MP4OutputFormat</p>
<p>StreamCompatibility=&quot;Standard&quot;&gt;</p>
<p>&lt;VideoProfile&gt;</p>
<p>&lt;MainH264VideoProfile</p>
<p>BFrameCount=&quot;3&quot;</p>
<p>EntropyMode=&quot;Cabac&quot;</p>
<p>RDOptimizationMode=&quot;Speed&quot;</p>
<p>HadamardTransform=&quot;False&quot;</p>
<p>SubBlockMotionSearchMode=&quot;Speed&quot;</p>
<p>MultiReferenceMotionSearchMode=&quot;Balanced&quot;</p>
<p>ReferenceBFrames=&quot;False&quot;</p>
<p>AdaptiveBFrames=&quot;True&quot;</p>
<p>SceneChangeDetector=&quot;True&quot;</p>
<p>FastIntraDecisions=&quot;False&quot;</p>
<p>FastInterDecisions=&quot;False&quot;</p>
<p>SubPixelMode=&quot;Quarter&quot;</p>
<p>SliceCount=&quot;0&quot;</p>
<p>KeyFrameDistance=&quot;00:00:02&quot;</p>
<p>InLoopFilter=&quot;True&quot;</p>
<p>MEPartitionLevel=&quot;EightByEight&quot;</p>
<p>ReferenceFrames=&quot;4&quot;</p>
<p>SearchRange=&quot;64&quot;</p>
<p>AutoFit=&quot;True&quot;</p>
<p>Force16Pixels=&quot;False&quot;</p>
<p>FrameRate=&quot;0&quot;</p>
<p>SeparateFilesPerStream=&quot;True&quot;</p>
<p>SmoothStreaming=&quot;False&quot;</p>
<p>NumberOfEncoderThreads=&quot;0&quot;&gt;</p>
<p>&lt;Streams</p>
<p>AutoSize=&quot;False&quot;</p>
<p>FreezeSort=&quot;False&quot;&gt;</p>
<p>&lt;StreamInfo</p>
<p>Size=&quot;1280, 720&quot;&gt;</p>
<p>&lt;Bitrate&gt;</p>
<p>&lt;ConstantBitrate</p>
<p>Bitrate=&quot;4500&quot;</p>
<p>IsTwoPass=&quot;False&quot;</p>
<p>BufferWindow=&quot;00:00:05&quot; /&gt;</p>
<p>&lt;/Bitrate&gt;</p>
<p>&lt;/StreamInfo&gt;</p>
<p>&lt;/Streams&gt;</p>
<p>&lt;/MainH264VideoProfile&gt;</p>
<p>&lt;/VideoProfile&gt;</p>
<p><strong>&lt;AudioProfile&gt;</strong></p>
<p><strong>&lt;DolbyDigitalPlusAudioProfile</strong></p>
<p><strong>Codec=&quot;DolbyDigitalPlus&quot;</strong></p>
<p><strong>EncoderMode=&quot;DolbyDigitalPlus&quot;</strong></p>
<p><strong>AudioCodingMode=&quot;Mode32&quot;</strong></p>
<p><strong>LFEOn=&quot;True&quot;</strong></p>
<p><strong>SamplesPerSecond=&quot;48000&quot;</strong></p>
<p><strong>BandwidthLimitingLowpassFilter=&quot;True&quot;</strong></p>
<p><strong>DialogNormalization=&quot;-31&quot;&gt;</strong></p>
<p><strong>&lt;Bitrate&gt;</strong></p>
<p><strong>&lt;ConstantBitrate</strong></p>
<p><strong>Bitrate=&quot;512&quot;</strong></p>
<p><strong>IsTwoPass=&quot;False&quot;</strong></p>
<p><strong>BufferWindow=&quot;00:00:00&quot; /&gt;</strong></p>
<p><strong>&lt;/Bitrate&gt;</strong></p>
<p><strong>&lt;/DolbyDigitalPlusAudioProfile&gt;</strong></p>
<p><strong>&lt;/AudioProfile&gt; </strong></p>
<p>&lt;/MP4OutputFormat&gt;</p>
<p>&lt;/OutputFormat&gt;</p>
<p>&lt;/MediaFile&gt;</p>
<p>&lt;/Preset&gt;</p>
<p>&lt;/Presets&gt;</p></li>
<li><p>Add <strong>DolbySendGridStep</strong> configuration</p>
<p>Insert a configuration record in <strong>ButlerConfiguration</strong> Table Storage with this data</p>
<p>Example:</p>
<p>a. <strong>PartitioKey</strong>: MediaButler.Common.workflow.ProcessHandler</p>
<p>b. <strong>RowKey</strong>: <em><strong>DolbySendGridStep</strong>.StepConfig</em></p>
<p>c. <strong>ConfigurationValue</strong>:</p>
<p><em>{</em></p>
<p><em>&quot;UserName&quot;:&quot;[your user account]&quot;,</em></p>
<p><em>&quot;Pswd&quot;:&quot;[upy password]&quot;,</em></p>
<p><em>&quot;To&quot;:&quot;[to mail]&quot;,</em></p>
<p><em>&quot;FromName&quot;: &quot;Butler Media Framework: Dolby&quot;,</em></p>
<p><em>&quot;FromMail&quot;: &quot;butler@XXX.com&quot; </em></p>
<p><em>}</em></p></li>
<li><p>Update <strong>ContainersToScan</strong> record in <strong>ButlerConfiguration</strong> storage Table, adding the new container.</p>
<p>Example:</p></li>
</ol>
<ul>
<li><p><em><strong>PrimaryKey</strong>: MediaButler.Workflow.WorkerRole</em></p></li>
<li><p><em><strong>RowKey</strong>: ContainersToScan</em></p></li>
<li><p><em><strong>ConfigurationValue</strong>: testbasicprocess, testpremiunencoder,<strong>mydolby</strong></em></p></li>
</ul>
<ol style="list-style-type: decimal">
<li><p>Restart <strong>Watcher</strong> Role from Media Butler Framework</p></li>
</ol>
<h2 id="testing-the-process">Testing the process</h2>
<p>To test the new process you need the following:</p>
<ul>
<li><p>A sample video (mezzanine video file) to upload. (i.e an MP4 file) You can <a href="http://d28c.wpc.azureedge.net/80D28C/amsorigin/e1893d2b-6a8c-4603-864c-0ed95a7cf42d/Silent_1920x1080_51AAC.mp4">download the source MP4</a> file here to use in your own testing.</p></li>
<li><p>You must upload the video file in the Media Butler “Incoming” blob folder.</p></li>
</ul>
<p>Now follow these steps:</p>
<ol style="list-style-type: decimal">
<li><p>Create a Blob folder container Incoming</p></li>
<li><p>Upload the video Mezzanine file blob storage container “<strong>Incoming</strong>”</p></li>
<li><p>Check the new Assets and jobs created in AMS</p></li>
<li><p>Wait to finish the transcoding process</p></li>
<li><p>At the end of the process, you will receive a notification email like this:</p></li>
</ol>
<blockquote>
<p><img src=".\mail.jpg" alt="" /></p>
</blockquote>
<h2 id="related-content">Related content</h2>
<ul>
<li><p><a href="https://github.com/liarjo/MediaBlutlerTest01/blob/master/README.md">Media Butler Framework repository</a></p></li>
</ul>
