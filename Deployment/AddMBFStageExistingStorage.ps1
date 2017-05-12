param(
    [Parameter(Mandatory=$true)]
    [string] $MediaServiceAccountName,
    [Parameter(Mandatory=$true)]
    [string] $MediaServiceAccountKey,
    [Parameter(Mandatory=$true)]
    [string] $MediaServiceStorageName,
    [Parameter(Mandatory=$true)]
    [string] $MediaServiceStorageKey,
    [Parameter(Mandatory=$true)]
    [string] $MBFStageStorageName,
    [Parameter(Mandatory=$true)]
    [string] $MBFStorageKey
)



function createProcessTestBasicProcess(){
    $butlerContainerStageName="testbasicprocess"
    $context=$butlerContainerStageName + ".Context"
    $chain=$butlerContainerStageName + ".ChainConfig"

    $processChain="[{""AssemblyName"":""MediaButler.BaseProcess.dll"",""TypeName"":""MediaButler.BaseProcess.MessageHiddeControlStep"",""ConfigKey"":""""},{""AssemblyName"":""MediaButler.BaseProcess.dll"",""TypeName"":""MediaButler.BaseProcess.IngestMultiMezzamineFilesStep"",""ConfigKey"":""""},{""AssemblyName"":""MediaButler.BaseProcess.dll"",""TypeName"":""MediaButler.BaseProcess.StandarEncodeStep"",""ConfigKey"":""StandarEncodeStep""},{""AssemblyName"":""MediaButler.BaseProcess.dll"",""TypeName"":""MediaButler.BaseProcess.DeleteOriginalAssetStep"",""ConfigKey"":""""},{""AssemblyName"":""MediaButler.BaseProcess.dll"",""TypeName"":""MediaButler.BaseProcess.CreateStreamingLocatorStep"",""ConfigKey"":""""},{""AssemblyName"":""MediaButler.BaseProcess.dll"",""TypeName"":""MediaButler.BaseProcess.CreateSasLocatorStep"",""ConfigKey"":""""},{""AssemblyName"":""MediaButler.BaseProcess.dll"",""TypeName"":""MediaButler.BaseProcess.SendMessageBackStep"",""ConfigKey"":""""},{""AssemblyName"":""MediaButler.BaseProcess.dll"",""TypeName"":""MediaButler.BaseProcess.MessageHiddeControlStep"",""ConfigKey"":""""}]"

    New-AzureStorageContainer -Name $butlerContainerStageName -Context $MBFStageStorageContext -Permission Off

    InsertButlerConfig -PartitionKey "MediaButler.Common.workflow.ProcessHandler" -RowKey $context -value "{""AssemblyName"":""MediaButler.BaseProcess.dll"",""TypeName"":""MediaButler.BaseProcess.ButlerProcessRequest"",""ConfigKey"":""""}" -accountName $MBFStageStorageName -accountKey $MBFStorageKey -tableName "ButlerConfiguration"  
    InsertButlerConfig -PartitionKey "MediaButler.Common.workflow.ProcessHandler" -RowKey $chain -value $processChain -accountName $MBFStageStorageName -accountKey $MBFStorageKey -tableName "ButlerConfiguration"  
    InsertButlerConfig -PartitionKey "MediaButler.Workflow.WorkerRole" -RowKey "ContainersToScan" -value $butlerContainerStageName -accountName $MBFStageStorageName -accountKey $MBFStorageKey -tableName "ButlerConfiguration"
}
Function InsertButlerConfig($accountName,$accountKey,$tableName, $PartitionKey,$RowKey,$value){
  	#Create instance of storage credentials object using account name/key
	$accountCredentials = New-Object "Microsoft.WindowsAzure.Storage.Auth.StorageCredentials" $accountName, $accountKey
	#Create instance of CloudStorageAccount object
	$storageAccount = New-Object "Microsoft.WindowsAzure.Storage.CloudStorageAccount" $accountCredentials, $true
	#Create table client
	$tableClient = $storageAccount.CreateCloudTableClient()
	#Get a reference to CloudTable object
	$table = $tableClient.GetTableReference($tableName)
	#Try to create table if it does not exist
	$table.CreateIfNotExists()
  
  	$entity = New-Object "Microsoft.WindowsAzure.Storage.Table.DynamicTableEntity" $PartitionKey, $RowKey
    $entity.Properties.Add("ConfigurationValue", $value)
    $result = $table.Execute([Microsoft.WindowsAzure.Storage.Table.TableOperation]::Insert($entity))
}

function createStageStorage(){
# Create MBF Configuration Table
    New-AzureStorageTable -Context $MBFStageStorageContext -Name "ButlerConfiguration"

# Create QUEUE
    New-AzureStorageQueue -Name "butlerfailed" -Context $MBFStageStorageContext
    New-AzureStorageQueue -Name "butlersend" -Context $MBFStageStorageContext
    New-AzureStorageQueue -Name "butlersuccess" -Context $MBFStageStorageContext
#Create BIN container
    New-AzureStorageContainer -Name "mediabutlerbin" -Context $MBFStageStorageContext -Permission Off
# Insert MBF configuration
    InsertButlerConfig -PartitionKey "MediaButler.Common.workflow.ProcessHandler" -RowKey "IsMultiTask" -value "1" -accountName $MBFStageStorageName -accountKey $MBFStorageKey -tableName "ButlerConfiguration"  
    InsertButlerConfig -PartitionKey "MediaButler.Workflow.ButlerWorkFlowManagerWorkerRole" -RowKey "roleconfig" -value "{""MaxCurrentProcess"":1,""SleepDelay"":5,""MaxDequeueCount"":1}" -accountName $MBFStageStorageName -accountKey $MBFStorageKey -tableName "ButlerConfiguration"  
    InsertButlerConfig -PartitionKey "general" -RowKey "BlobWatcherPollingSeconds" -value "5" -accountName $MBFStageStorageName -accountKey $MBFStorageKey -tableName "ButlerConfiguration"  
    InsertButlerConfig -PartitionKey "general" -RowKey "FailedQueuePollingSeconds" -value "5" -accountName $MBFStageStorageName -accountKey $MBFStorageKey -tableName "ButlerConfiguration"  
    InsertButlerConfig -PartitionKey "general" -RowKey "MediaServiceAccountName" -value $MediaServiceAccountName -accountName $MBFStageStorageName -accountKey $MBFStorageKey -tableName "ButlerConfiguration"  
    InsertButlerConfig -PartitionKey "general" -RowKey "MediaStorageConn" -value $MBFMediaServiceStorageConn -accountName $MBFStageStorageName -accountKey $MBFStorageKey -tableName "ButlerConfiguration"  
    InsertButlerConfig -PartitionKey "general" -RowKey "PrimaryMediaServiceAccessKey" -value $MediaServiceAccountKey -accountName $MBFStageStorageName -accountKey $MBFStorageKey -tableName "ButlerConfiguration"  
    InsertButlerConfig -PartitionKey "general" -RowKey "SuccessQueuePollingSeconds" -value "5" -accountName $MBFStageStorageName -accountKey $MBFStorageKey -tableName "ButlerConfiguration"  
    #Incoming Filter config Sample
    InsertButlerConfig -PartitionKey "MediaButler.Workflow.WorkerRole" -RowKey "FilterPatterns" -value ".test,.mp5" -accountName $MBFStageStorageName -accountKey $MBFStorageKey -tableName "ButlerConfiguration"  
}

        
        ############
        $MBFStageStorageContext=New-AzureStorageContext -StorageAccountKey $MBFStorageKey -StorageAccountName $MBFStageStorageName
        $MBFMediaServiceStorageConn=$("DefaultEndpointsProtocol=https;AccountName=$MediaServiceStorageName;AccountKey=$MediaServiceStorageKey")
        
        #7. Create MBF Storage 
        createStageStorage

        #8. Create Sample process on MBF configuration
        createProcessTestBasicProcess




