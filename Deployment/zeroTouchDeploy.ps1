

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
    [string] $SubscriptionName,
    [string] $MyClearTextUsername="",
    [string] $MyClearTextPassword="",
    [Parameter(Mandatory=$true)]
    [string] $appName,
    [string] $appRegion="East US",
    [string] $overWriteRG=$false
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
function login(){
    $MyUsernameDomain=$MyClearTextUsername

    $SecurePassword=Convertto-SecureString –String $MyClearTextPassword –AsPlainText –force

    $MyCredentials=New-object System.Management.Automation.PSCredential $MyUsernameDomain,$SecurePassword
    
    Login-AzureRmAccount -Credential $MyCredentials
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
function Get-AzureRmWebAppPublishingCredentials($resourceGroupName, $webAppName, $slotName = $null){
	if ([string]::IsNullOrWhiteSpace($slotName)){
		$resourceType = "Microsoft.Web/sites/config"
		$resourceName = "$webAppName/publishingcredentials"
	}
	else{
		$resourceType = "Microsoft.Web/sites/slots/config"
		$resourceName = "$webAppName/$slotName/publishingcredentials"
	}
	$publishingCredentials = Invoke-AzureRmResourceAction -ResourceGroupName $resourceGroupName -ResourceType $resourceType -ResourceName $resourceName -Action list -ApiVersion 2015-08-01 -Force
    return $publishingCredentials
}
function Get-KuduApiAuthorisationHeaderValue($resourceGroupName, $webAppName, $slotName = $null){
    $publishingCredentials = Get-AzureRmWebAppPublishingCredentials $resourceGroupName $webAppName $slotName
    return ("Basic {0}" -f [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(("{0}:{1}" -f $publishingCredentials.Properties.PublishingUserName, $publishingCredentials.Properties.PublishingPassword))))
}
function ExexuteKudoCommand($resourceGroupName,$webAppName,$command, $slotName = ""){

    $kuduApiAuthorisationToken =  Get-KuduApiAuthorisationHeaderValue -resourceGroupName $resourceGroupName -webAppName $webAppName -slotName $slotName

    if ([string]::IsNullOrWhiteSpace($slotName)){
        $kuduApiUrl = "https://$webAppName.scm.azurewebsites.net/api/command"
    }
    else{
        $kuduApiUrl = "https://$webAppName`-$slotName.scm.azurewebsites.net/api/command"
    }
    $commandBody = @{ 
        command = $command
        dir = "site\\wwwroot"
        }

    $apiReturn = Invoke-RestMethod -Uri $kuduApiUrl -Headers @{"Authorization"=$kuduApiAuthorisationToken;"If-Match"="*"} -Method POST -ContentType "application/json" -Body  (ConvertTo-Json $commandBody)
    Write-Host $apiReturn
}
function Upload-ZipToWebApp($resourceGroupName,$webAppName, $slotName = "", $localPath, $kuduPath){

    $kuduApiAuthorisationToken =  Get-KuduApiAuthorisationHeaderValue -resourceGroupName $resourceGroupName -webAppName $webAppName -slotName $slotName
   
    if ([string]::IsNullOrWhiteSpace($slotName)){
        $kuduApiUrl = "https://$webAppName.scm.azurewebsites.net/api/zip/site/wwwroot/$kuduPath"
    }
    else{
        $kuduApiUrl = "https://$webAppName`-$slotName.scm.azurewebsites.net/api/zip/site/wwwroot/$kuduPath"
    }
    $virtualPath = $kuduApiUrl.Replace(".scm.azurewebsites.", ".azurewebsites.").Replace("/api/zip/site/wwwroot", "")
   

    $apiReturn = Invoke-RestMethod -Uri $kuduApiUrl -Headers @{"Authorization"=$kuduApiAuthorisationToken;"If-Match"="*"} -Method PUT -InFile $localPath -ContentType "multipart/form-data" 
    
    Write-Host $apiReturn
}

# 0 Set Constants
    Set-Variable TemplateFileURI 'https://raw.githubusercontent.com/DX-TED-GEISV-Americas/Media-Butler-Framework/master/Deployment/mbfAzureDeploy.json' -Option ReadOnly -Force

#1. Login With Organizational Account 
    IF([string]::IsNullOrEmpty($MyClearTextUsername)) 
    {
        Write-Host "[Login] Not Login, just use the current user"   
    }
    else
    {
        Write-Host "[Login]  Login,  use the  user"
        Write-Host $MyClearTextUsername    
        login
    }
    

#2. Select Subscription
    Select-AzureRmSubscription -SubscriptionName $SubscriptionName 

    

#3. Create Resource Group
#check ResourceGroup

    $listrg=Get-AzureRmResourceGroup | Select-Object -Property ResourceGroupName
    $existRG=($listrg.ResourceGroupName -contains $appName)

#4. Resource group light traffic control 
    if (($overWriteRG -eq $false) -and ($existRG))
    {
        $auxSwitch= $true
    }
    else
    {
        $auxSwitch=$false
    }

    if ($auxSwitch)
    {
        #not use the Existing RG
         Write-Host "Resource Group allready exist"
         Write-Host "finish Execution"
    }
    else
    {
        #RG
        if (-not $existRG)
        {
            #RG Not exist
            $myResourceGroup=New-AzureRmResourceGroup -Name $appName -Location $appRegion
        }
        else
        {
            #RG exist
            $myResourceGroup=Get-AzureRmResourceGroup -Name $appName
        }

 
        #5. Deploy MBF Host       
        $name=(((Get-Date).ToUniversalTime()).ToString('YYMMdd'))
        $OptionalParameters = New-Object -TypeName Hashtable
        $today=Get-Date -UFormat "%Y%m%d"
        $OptionalParameters.Add("deployDate",$today)
       
        #$deployOutPut = New-AzureRmResourceGroupDeployment -Name $name -ResourceGroupName $myResourceGroup.ResourceGroupName -TemplateUri $TemplateFileURI   @OptionalParameters -Force -Verbose
        $deployOutPut = New-AzureRmResourceGroupDeployment -Name $name -ResourceGroupName $myResourceGroup.ResourceGroupName -TemplateFile "C:\Users\jpgarcia\Desktop\New folder\mbfAzureDeploy.json"   @OptionalParameters -Force -Verbose

        #6. Create MBF Storage Tables, queues and basic configuration
        $MBFStageStorageName=$deployOutPut.Outputs.mbfStagingStorageName.Value
        $MBFStorageKey=$deployOutPut.Outputs.mbfStagingStorageKey.Value
        $MBFStageStorageContext=New-AzureStorageContext -StorageAccountKey $MBFStorageKey -StorageAccountName $MBFStageStorageName
        $MBFMediaServiceStorageConn=$("DefaultEndpointsProtocol=https;AccountName=$MediaServiceStorageName;AccountKey=$MediaServiceStorageKey")
        
        #7. Create MBF Storage 
        createStageStorage

        #8. Create Sample process on MBF configuration
        createProcessTestBasicProcess

        #9. Deploy MBF Webjob
        ExexuteKudoCommand -resourceGroupName $myResourceGroup.ResourceGroupName -webAppName $deployOutPut.Outputs.webAppXName.Value -command "mkdir App_Data\jobs\continuous" -slotName $slotName
        #Upload Function
        Upload-ZipToWebApp -resourceGroupName $myResourceGroup.ResourceGroupName -webAppName $deployOutPut.Outputs.webAppXName.Value -slotName $slotName -localPath "C:\tmp\MediaButlerWebJob.zip" -kuduPath 'App_Data/jobs/continuous'

        #10. ENd SCRIPT
        Write-Host ("Media Bulter Framework deployments objetcs:")
        Write-Host ("1. Stage Storage Account Name {0}" -f $MBFStageStorageName )
        Write-Host ("2. Stage Storage Account Key {0}" -f $MBFStorageKey)
        Write-Host ("3. Web application name  {0}" -f $deployOutPut.Outputs.webAppXName.Value)
        Write-Host ("3. Web plan name  {0}" -f $deployOutPut.Outputs.farmplanName.Value)


        
        $tagetURL="http://{0}.azurewebsites.net/" -f $deployOutPut.Outputs.webAppXName.Value

        start $tagetURL

}