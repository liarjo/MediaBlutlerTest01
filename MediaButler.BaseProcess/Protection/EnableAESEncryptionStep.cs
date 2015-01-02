using Microsoft.WindowsAzure.MediaServices.Client;
using Microsoft.WindowsAzure.MediaServices.Client.ContentKeyAuthorization;
using Microsoft.WindowsAzure.MediaServices.Client.DynamicEncryption;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MediaButler.BaseProcess
{
    class AESEncryptionData
    {
        public string sampleAudience;
        public string sampleIssuer;
        public string policyName;

    }
    class EnableAESEncryptionStep : MediaButler.Common.workflow.StepHandler
    {
        private ButlerProcessRequest myRequest;
        private CloudMediaContext _MediaServiceContext;
        private AESEncryptionData myConfig;
        private byte[] GetRandomBuffer(int size)
        {
            byte[] randomBytes = new byte[size];
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(randomBytes);
            }

            return randomBytes;
        }
        private IContentKey CreateEnvelopeTypeContentKey(IAsset asset)
        {
            // Create envelope encryption content key
            Guid keyId = Guid.NewGuid();
            byte[] contentKey = GetRandomBuffer(16);
            IContentKey key = _MediaServiceContext.ContentKeys.Create(keyId, contentKey, "ContentKey", ContentKeyType.EnvelopeEncryption);
            // Associate the key with the asset.
            asset.ContentKeys.Add(key);
            Trace.TraceInformation("Created key {0} for the asset {1} ", key.Id, asset.Id);
            return key;
        }
        private string GenerateTokenRequirements()
        {
            //TokenRestrictionTemplate template = new TokenRestrictionTemplate();
            //template.PrimaryVerificationKey = new SymmetricVerificationKey();
            //template.AlternateVerificationKeys.Add(new SymmetricVerificationKey());
            //template.Audience = new Uri( myConfig.sampleAudience);
            //template.Issuer = new Uri( myConfig.sampleIssuer) ;
            //template.RequiredClaims.Add(TokenClaim.ContentKeyIdentifierClaim);
            //return TokenRestrictionTemplateSerializer.Serialize(template);
            string primarySymmetricKey = "8sAEcbUnsHJOnMpRU0f7z4gAROCoQ3ailkK5ARIt12iDxOu5KTf5mihIbims0uJHTTQQSkO/W+WDTEdFFH7rug==";
            string secondarySymmetricKey = "vga/o4PJT+IQAaH1Po0uhBrL7aoUu0prBTO9jmWsk71CFtPwBJiwwBiWMN45NGXp3rDIIi81E3QU0dXg7pmDxw==";
            TokenRestrictionTemplate objTokenRestrictionTemplate = new TokenRestrictionTemplate();
            objTokenRestrictionTemplate.PrimaryVerificationKey = new SymmetricVerificationKey(Convert.FromBase64String(primarySymmetricKey));
            objTokenRestrictionTemplate.AlternateVerificationKeys.Add(new SymmetricVerificationKey(Convert.FromBase64String(secondarySymmetricKey)));
            objTokenRestrictionTemplate.Audience = new Uri(myConfig.sampleAudience);
            objTokenRestrictionTemplate.Issuer = new Uri(myConfig.sampleIssuer);
            return TokenRestrictionTemplateSerializer.Serialize(objTokenRestrictionTemplate);
        }
        private string AddTokenRestrictedAuthorizationPolicy(IContentKey contentKey)
        {
            string tokenTemplateString = GenerateTokenRequirements();

            IContentKeyAuthorizationPolicy policy = _MediaServiceContext.
                                    ContentKeyAuthorizationPolicies.
                                    CreateAsync(myConfig.policyName).Result;

            List<ContentKeyAuthorizationPolicyRestriction> restrictions =
                    new List<ContentKeyAuthorizationPolicyRestriction>();

            ContentKeyAuthorizationPolicyRestriction restriction =
                    new ContentKeyAuthorizationPolicyRestriction
                    {
                        Name = "Token Authorization Policy",
                        KeyRestrictionType = (int)ContentKeyRestrictionType.TokenRestricted,
                        Requirements = tokenTemplateString
                    };

            restrictions.Add(restriction);

            //You could have multiple options 
            IContentKeyAuthorizationPolicyOption policyOption =
                _MediaServiceContext.ContentKeyAuthorizationPolicyOptions.Create(
                    "Token option for HLS",
                    ContentKeyDeliveryType.BaselineHttp,
                    restrictions,
                    null  // no key delivery data is needed for HLS
                    );

            policy.Options.Add(policyOption);

            // Add ContentKeyAutorizationPolicy to ContentKey
            contentKey.AuthorizationPolicyId = policy.Id;
            IContentKey updatedKey = contentKey.UpdateAsync().Result;
            Trace.TraceInformation("Adding Key to Asset: Key ID is " + updatedKey.Id);

            return tokenTemplateString;
        }
        private void CreateAssetDeliveryPolicy(IAsset asset, IContentKey key)
        {
            Uri keyAcquisitionUri = key.GetKeyDeliveryUrl(ContentKeyDeliveryType.BaselineHttp);

            string envelopeEncryptionIV = Convert.ToBase64String(GetRandomBuffer(16));

            // The following policy configuration specifies: 
            //   key url that will have KID=<Guid> appended to the envelope and
            //   the Initialization Vector (IV) to use for the envelope encryption.
            Dictionary<AssetDeliveryPolicyConfigurationKey, string> assetDeliveryPolicyConfiguration =
                new Dictionary<AssetDeliveryPolicyConfigurationKey, string> 
                {
                    {AssetDeliveryPolicyConfigurationKey.EnvelopeKeyAcquisitionUrl, keyAcquisitionUri.ToString()},
                    {AssetDeliveryPolicyConfigurationKey.EnvelopeEncryptionIVAsBase64, envelopeEncryptionIV}
                };

            IAssetDeliveryPolicy assetDeliveryPolicy =
                _MediaServiceContext.AssetDeliveryPolicies.Create(
                            "myAssetDeliveryPolicy",
                            AssetDeliveryPolicyType.DynamicEnvelopeEncryption,
                            AssetDeliveryProtocol.SmoothStreaming | AssetDeliveryProtocol.HLS,
                            assetDeliveryPolicyConfiguration);

            // Add AssetDelivery Policy to the asset
            asset.DeliveryPolicies.Add(assetDeliveryPolicy);

            Trace.TraceInformation("Adding Asset Delivery Policy: " + assetDeliveryPolicy.AssetDeliveryPolicyType);
        }

        private void Setup()
        {

            myConfig = Newtonsoft.Json.JsonConvert.DeserializeObject<AESEncryptionData>(this.StepConfiguration);

        }
        public override void HandleExecute(Common.workflow.ChainRequest request)
        {
            myRequest = (ButlerProcessRequest)request;
            Setup();
            _MediaServiceContext = new CloudMediaContext(myRequest.MediaAccountName, myRequest.MediaAccountKey);
            IAsset encodedAsset = (from m in _MediaServiceContext.Assets select m).Where(m => m.Id == myRequest.AssetId).FirstOrDefault();

            //Create key
            IContentKey key = CreateEnvelopeTypeContentKey(encodedAsset);
            //Create Token Template
            string tokenTemplateString = AddTokenRestrictedAuthorizationPolicy(key);
            Trace.TraceInformation("Added authorization policy: {0}", key.AuthorizationPolicyId);
            //create Delivery Policy
            CreateAssetDeliveryPolicy(encodedAsset, key);

            if (!String.IsNullOrEmpty(tokenTemplateString))
            {
                // Deserializes a string containing an Xml representation of a TokenRestrictionTemplate
                // back into a TokenRestrictionTemplate class instance.
                TokenRestrictionTemplate tokenTemplate = TokenRestrictionTemplateSerializer.Deserialize(tokenTemplateString);

                // Generate a test token based on the data in the given TokenRestrictionTemplate.
                // Note, you need to pass the key id Guid because we specified 
                // TokenClaim.ContentKeyIdentifierClaim in during the creation of TokenRestrictionTemplate.
                Guid rawkey = EncryptionUtils.GetKeyIdAsGuid(key.Id);
                string testToken = TokenRestrictionTemplateSerializer.GenerateTestToken(tokenTemplate, null, rawkey);
                Trace.TraceInformation("The authorization token is:\n{0}", testToken);
                myRequest.Log.Add("The authorization token");
                myRequest.Log.Add(testToken);
                myRequest.Log.Add("");

            }
        }

        public override void HandleCompensation(Common.workflow.ChainRequest request)
        {
            Trace.TraceWarning("{0} in process {1} processId {2} has not HandleCompensation", this.GetType().FullName, myRequest.ProcessTypeId, myRequest.ProcessInstanceId);
        }
    }
}
