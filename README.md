<h1>B2C Custom policy using Azure function for AAD groups information</h1>

<!-- wp:paragraph -->
<p>AAD B2C is more than just an identity as a service, it is a customer identity access management (CIAM) solution but to unleash its true power, you have to get into custom policies and write some XML.</p>
<!-- /wp:paragraph -->

<!-- wp:paragraph -->
<p>I have come across scenarios where customers don't consider B2C if its not offered by OOB User Flows and the most common ask is for App Roles and AAD groups. Taking that into consideration, i have worked on this scenario which provides an easy means of getting AAD group membership for a user.</p>
<!-- /wp:paragraph -->

<!-- wp:paragraph -->
<p>The code provided includes Azure Function and B2C custom policies. There are some <a href="https://docs.microsoft.com/en-us/azure/active-directory-b2c/tutorial-create-user-flows?pivots=b2c-custom-policy#prerequisites" data-type="URL" data-id="https://docs.microsoft.com/en-us/azure/active-directory-b2c/tutorial-create-user-flows?pivots=b2c-custom-policy#prerequisites">perquisites</a> for IEF which are well documented already.</p>
<!-- /wp:paragraph -->

<!-- wp:heading -->
<h2>Scenario</h2>
<!-- /wp:heading -->

<!-- wp:image {"id":27,"sizeSlug":"large","linkDestination":"none"} -->
<figure class="wp-block-image size-large"><img src="https://sabih114253105.files.wordpress.com/2021/09/b2c-cp-rest-1.png?w=859" alt="" class="wp-image-27"/></figure>
<!-- /wp:image -->

<!-- wp:paragraph -->
<p>Objective is to get a token which have a list of groups that the user is member of. The way I have implemented this, is by using B2C Custom Policies which calls an Azure Function through a Technical Profile. Azure Function makes use of Microsoft Graph to obtain a list of groups which is passed back to the Technical Profile.</p>
<!-- /wp:paragraph -->

<!-- wp:heading -->
<h2>Azure Function</h2>
<!-- /wp:heading -->

<!-- wp:paragraph -->
<p>Azure Function utilizes an App registration in Azure AD (B2C) in order get the group membership of the user. Therefore, we need an App registration with Application permissions to support this. For details on permissions check <a href="https://docs.microsoft.com/en-us/graph/api/user-getmembergroups?view=graph-rest-1.0&tabs=http" data-type="URL" data-id="https://docs.microsoft.com/en-us/graph/api/user-getmembergroups?view=graph-rest-1.0&tabs=http">user: getMemberGroups</a>. Once we have the App registration, we can set the values in local.settings.json file which is good enough to run Azure Function locally but when you publish it, you will have to update the configuration section of Azure Function in the portal and add application settings with the same variables and values.</p>
<!-- /wp:paragraph -->

<!-- wp:code {"style":{"typography":{"fontSize":"10px"}}} -->
<pre class="wp-block-code" style="font-size:10px"><code>{
    "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "DefaultEndpointsProtocol=https;AccountName=storageacname;AccountKey=xxxxxxxxxxx;EndpointSuffix=core.windows.net",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
<strong><em><span class="has-inline-color has-primary-color">    "Instance": "https://login.microsoftonline.com/{0}",
    "ApiUrl": "https://graph.microsoft.com/",
    "Dest_Tenant": "xxxxxxx-xxx-xxxx-xxxx-xxxxxxxxxx",
    "Dest_ClientId": "xxxxxxx-xxxxx-xxxx-xxxx-xxxxxxxxxxx",
    "Dest_ClientSecret": "xxxxxxxxxxxxxxxx"</span></em></strong>
  }
}</code></pre>
<!-- /wp:code -->

<!-- wp:heading -->
<h2>B2C Custom Policies</h2>
<!-- /wp:heading -->

<!-- wp:paragraph -->
<p>I have used <a href="https://github.com/Azure-Samples/active-directory-b2c-custom-policy-starterpack" data-type="URL" data-id="https://github.com/Azure-Samples/active-directory-b2c-custom-policy-starterpack">B2C starter pack</a> to begin with because you don't want to write these files from the scratch. There are multiple folders in that, for simplicity I am using the LocalAccounts.</p>
<!-- /wp:paragraph -->

<!-- wp:paragraph -->
<p>The three files which we need to test our scenario are SignUpOrSignin.xml (aka RP file), TrustFrameworkExtensions.xml (aka Extensions file) and TrustFrameworkBase.xml (aka Base file). Most of the modification is done in the TrustFrameworkExtensions.xml file.</p>
<!-- /wp:paragraph -->

<!-- wp:heading {"level":5} -->
<h5>User Journey</h5>
<!-- /wp:heading -->

<!-- wp:paragraph -->
<p>To start with, I have cut the signUporSignIn UserJourney from the Base file into the Extensions file and added an Orchestration step which references a Technical Profile called "REST-CallFuncApp". I have adjusted the order number accordingly.</p>
<!-- /wp:paragraph -->

<!-- wp:code {"style":{"typography":{"fontSize":"10px"}}} -->
<pre class="wp-block-code" style="font-size:10px"><code>&lt;OrchestrationStep Order="4" Type="ClaimsExchange">
          &lt;ClaimsExchanges>
            &lt;ClaimsExchange Id="RESTCallFuncApp" TechnicalProfileReferenceId="REST-CallFuncApp" />
          &lt;/ClaimsExchanges>
        &lt;/OrchestrationStep></code></pre>
<!-- /wp:code -->

<!-- wp:heading {"level":5} -->
<h5>Technical Profile</h5>
<!-- /wp:heading -->

<!-- wp:paragraph -->
<p>Technical profile is just like a function which takes input claims and may provide some output claims after processing. In our case, we are calling an Azure Function which is handled by RestfulProvider. Each Technical Profile will have a protocol and the handler which we dont have to configure in this case. However, we have to configure the metadata and input/output claims. In this case input claim is what we are sending to Azure Function and metadata defines the configuration related to REST API call.</p>
<!-- /wp:paragraph -->

<!-- wp:code {"style":{"typography":{"fontSize":"10px"}}} -->
<pre class="wp-block-code" style="font-size:10px"><code>&lt;TechnicalProfile Id="REST-CallFuncApp">
          &lt;DisplayName>Return groups claim&lt;/DisplayName>
          &lt;Protocol Name="Proprietary" Handler="Web.TPEngine.Providers.RestfulProvider, Web.TPEngine, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null" />
          &lt;Metadata>
            &lt;Item Key="ServiceUrl">https://b2crestfull.azurewebsites.net/api/FnB2CFindUserGroups?code=yourFunctionAccessKey&lt;/Item>
            &lt;Item Key="SendClaimsIn">Body&lt;/Item>
            &lt;!-- Set AuthenticationType to Basic or ClientCertificate in production environments -->
            &lt;Item Key="AuthenticationType">None&lt;/Item>
            &lt;!-- REMOVE the following line in production environments -->
            &lt;Item Key="AllowInsecureAuthInProduction">true&lt;/Item>
          &lt;/Metadata>
          &lt;InputClaims>
            &lt;InputClaim ClaimTypeReferenceId="objectId" />
          &lt;/InputClaims>
          &lt;OutputClaims>
            &lt;!-- Claims parsed from your REST API -->
            &lt;OutputClaim ClaimTypeReferenceId="groups" />
          &lt;/OutputClaims>
        &lt;/TechnicalProfile></code></pre>
<!-- /wp:code -->

<!-- wp:paragraph -->
<p>As output, we get the list of group Id's which we store in an output claim called groups which is of data type stringCollection. Anything in the output claim is available in the Claims Bag and can be sent back to the Relying Party.</p>
<!-- /wp:paragraph -->

<!-- wp:code {"style":{"typography":{"fontSize":"10px"}}} -->
<pre class="wp-block-code" style="font-size:10px"><code>&lt;BuildingBlocks>
    &lt;ClaimsSchema>
      &lt;ClaimType Id="groups">
        &lt;DisplayName>Your Groups&lt;/DisplayName>
        &lt;DataType>stringCollection&lt;/DataType>
      &lt;/ClaimType>
    &lt;/ClaimsSchema>
  &lt;/BuildingBlocks></code></pre>
<!-- /wp:code -->

<!-- wp:heading {"level":5} -->
<h5>Relying Party</h5>
<!-- /wp:heading -->

<!-- wp:paragraph -->
<p>Lastly, I have configured the Relying Party in the file SignUpOrSignin.xml to send groups claim in the jwt Token.</p>
<!-- /wp:paragraph -->

<!-- wp:code {"style":{"typography":{"fontSize":"10px"}}} -->
<pre class="wp-block-code" style="font-size:10px"><code>&lt;RelyingParty>
    &lt;DefaultUserJourney ReferenceId="SignUpOrSignIn" />
    &lt;TechnicalProfile Id="PolicyProfile">
      &lt;DisplayName>PolicyProfile&lt;/DisplayName>
      &lt;Protocol Name="OpenIdConnect" />
      &lt;OutputClaims>
        &lt;OutputClaim ClaimTypeReferenceId="displayName" />
        &lt;OutputClaim ClaimTypeReferenceId="givenName" />
        &lt;OutputClaim ClaimTypeReferenceId="surname" />
        &lt;OutputClaim ClaimTypeReferenceId="email" />
<strong><em><span class="has-inline-color has-primary-color">        &lt;OutputClaim ClaimTypeReferenceId="groups" DefaultValue="" /></span></em></strong>
        &lt;OutputClaim ClaimTypeReferenceId="objectId" PartnerClaimType="sub"/>
        &lt;OutputClaim ClaimTypeReferenceId="tenantId" AlwaysUseDefaultValue="true" DefaultValue="{Policy:TenantObjectId}" />
      &lt;/OutputClaims>
      &lt;SubjectNamingInfo ClaimType="sub" />
    &lt;/TechnicalProfile>
  &lt;/RelyingParty></code></pre>
<!-- /wp:code -->

<!-- wp:paragraph -->
<p>And now when you sign in to your application, you will get a Token with a claim called groups which will have your group Id's.</p>
<!-- /wp:paragraph -->

<!-- wp:code {"style":{"typography":{"fontSize":"10px"}}} -->
<pre class="wp-block-code" style="font-size:10px"><code><span style="color:#a30003" class="has-inline-color">{
  "typ": "JWT",
  "alg": "RS256",
  "kid": "hd0o7C4Fkbstrc-FpJ5y6zQvi1ekBjyELKHScfJ7pho"
}.</span><span class="has-inline-color has-primary-color">{
  "exp": 1630734610,
  "nbf": 1630731010,
  "ver": "1.0",
  "iss": "https://B2Ctenant.b2clogin.com/367886bf-2d02-48f8-xxxxxxxxxxxxxxx/v2.0/",
  "sub": "57b9d8f4-b5d7-424b-xxxxxxxxxxxxxxxx",
  "aud": "57c11075-4193-43b0-xxxxxxxxxxxxxxxx",
  "acr": "b2c_1a_signup_signin",
  "nonce": "defaultNonce",
  "iat": 1630731010,
  "auth_time": 1630731010,
  "name": "Alice Bob",
  "given_name": "Alice",
  "family_name": "Bob",
  "groups": &#91;
    "755f2a24-705d-4ae9-af01-47155b0abe99"
  ],
  "tid": "367886bf-2d02-48f8-xxxxxxxxxxxxxx"
}.</span><span style="color:#22a300" class="has-inline-color">&#91;Signature]</span></code></pre>
<!-- /wp:code -->

<!-- wp:paragraph -->
<p></p>
<!-- /wp:paragraph -->
