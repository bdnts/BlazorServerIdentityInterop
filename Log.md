# BlazorServerIdentityInterop

## Base 00.00.00 Create a Blank Blazor Project. 

## Base 00.01.00 Scaffold Identity 
* Override all files
* Create new context
* Optional use SqLite
* Create new User class
* Finish
* Compile
    * Fix error in RegisterConfirmation.cshtml.cs
    * Add `using Microsoft.AspNetCore.Identity;`
* Launch application
* No Sign Up / Sign In / Sign Out visibile (SUSISO) 

## Base 00.02.00 SUSISO Menu
* Need to augment the menu for SUSISO  options and display current Authentication State.

### App.razor
* Add the tag `<CascadingAuthenticationState>` around everything
```
<CascadingAuthenticationState>
    <Router AppAssembly="@typeof(Program).Assembly">
        <Found Context="routeData">
            <RouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)" />
        </Found>
        <NotFound>
            <LayoutView Layout="@typeof(MainLayout)">
                <p>Sorry, there's nothing at this address.</p>
            </LayoutView>
        </NotFound>
    </Router>
</CascadingAuthenticationState>
```
### LoginDisplay.razor
* Create Blazor component *Shared/LoginDisplay.razor**
* Paste in the following:
```c#
```
### Navmenu.razor
* Edit *MainLayout.razor* and add element *<LoginDisplay>*
    ```c#
    @inherits LayoutComponentBase

    <div class="sidebar">
        <NavMenu />
    </div>

    <div class="main">
        <div class="top-row px-4">
            <LoginDisplay />
            <a href="https://docs.microsoft.com/aspnet/" target="_blank">About</a>
        </div>

        <div class="content px-4">
            @Body
        </div>
    </div>
    ```
### Startup.cs
* Add `app.UseAuthentication();` and `app.UserAuthorization();`
```c#
    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseEndpoints(endpoints =>
```

### Compile and Test
* Clean up any issues
* Should see a Hello World similar to this:
  > ![B S I I A 010](BlazorServerIdentityInterop/wwwroot/images/BSII-A-010.PNG)
* Sign Up and Sign In are highlighted because they will use new Blazor Components to be built.
* The Register and Login are the Razor pages built by scaffolding.  We will keep them on the menu for the moment.
* Stop application

### Provision Database
* Package Manager Console
* `add-migration m1`

    ```
    PM> add-migration m1
    Build started...
    Build succeeded.
    To undo this action, use Remove-Migration.
    PM> 
    ```
* `update-database`
  
    ```
    PM> update-database
    Build started...
    Build succeeded.
    Done.
    PM> 
    ```
### Log Out
The legacy Logout uses a POST call with no data.  
Trouble is, it causes a `AntiForgeryValidationException`.
We need to disable AF for the moment.

### Logout.cshtml.cs
* Add an `[IgnoreAntiForgeryToken]` attribute to the top of the class.
    ```c#
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public class LogoutModel : PageModel
    ```

### Retest
* Launch app
* Use the legacy Register Link
* Register an account
* Confirm the account by clicking *Click here to confirm your account*
* Click on the Home link of the project name *BlazorServerIdentityInterop*
* Click on the legacy  *Log in* link.
* Proceed to Login
* The menu will change to should the username used (email), and Sign Out & Log out display.
  >![B S I I A 020](BlazorServerIdentityInterop/wwwroot/images/BSII-A-020.PNG) 
* Different menu items are displayed depending on the Authentication state.

## Base 00.03.00 Implement Interop to utilize cookies.
The following steps are changes needed to gain access to the AF token, and POST a form to Login.

### wwwroot/js/Interop.js
Borrowed from the Oqtane project (https://www.oqtane.org/) and 
Shaun Walker blog "Exploring Authentication in Blazor" (https://www.oqtane.org/Resources/Blog/PostId/527/exploring-authentication-in-blazor)

Only interested in 2 routines:

    ```
    getElementByName: function (name) {
        var elements = document.getElementsByName(name);
        if (elements.length) {
            return elements[0].value;
        } else {
            return "";
        }
    },
    submitForm: async function (path, fields) {
        const form = document.createElement('form');
        form.method = 'post';
        form.action = path;

        for (const key in fields) {
            if (fields.hasOwnProperty(key)) {
                const hiddenField = document.createElement('input');
                hiddenField.type = 'hidden';
                hiddenField.name = key;
                hiddenField.value = fields[key];
                form.appendChild(hiddenField);
            }
        }

        document.body.appendChild(form);
        var response = await form.submit();
    },
    ```
  * The `getElementByName` will be used to retrieve the AF token from the Document
  * The `submitForm` will be used to dynamically build and submit a form via a POST call. 

### Shared/Interop.cs
This file is a C# wrapper for the Javascript methods in Interop.js.  
Actual location is not hugely important, so it was placed in the Shared location.
The only 2 wrappers that will be used are `GetElementByName()` and `SubmitForm()`  
The full code is in the repo.

### Pages/Host.cshtml
* Add one call to embed the token in the client html
    ```html
    <body>
        @(Html.AntiForgeryToken())
        <app>
    ```
* Add the following line to load the *interop.js* file
    ```html
        <script src="_framework/blazor.server.js"></script>
        <script src="~/js/Interop.js"></script>
    </body>
    </html>
    ```

### SignIn
SignIn is the most difficult flow, so lets start there.  We will begin with supporting files.

#### SignIn.razor
* In Areas/Identity/Pages/Account, create SignIn.razor
* Copy the source code from the repository

#### Key elements
    ```c#
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                var interop = new Interop(_jsruntime);
                AntiForgeryToken = await interop.GetElementByName("__RequestVerificationToken");
            }
    ```
  * The first time time the page is rendered, a call is made to retrieve the AF token which is embedded in element *__RequestVerificationToken**
  * The result is stored in `AntiForgeryToken`
  
```
        var fields = new
        {
            __RequestVerificationToken = AntiForgeryToken
            , email = Input.Email,
            password = Input.Password,
            remember = Input.RememberMe,
            returnurl = "/SignIn"
        };
        var interop = new Interop(_jsruntime);
        await interop.SubmitForm("Identity/Account/Login", fields);
        HttpResponseMessage response = new HttpResponseMessage();
```
* `fields` is an anonymous array of key values.
* Notice has `AntiForgeryToken` is passed in.
* A call is made to `SubmitForm()`
  * Though this call is `await`ed, there is no reponse message.  So an empty one is created.
* The Javascript method will POST the data to Login, where it will be processed the same as if it came from the Login View, as we did in our earlier login.

### Test 
* Launch the app
* Click on the new ![B S I I B 010](BlazorServerIdentityInterop/wwwroot/images/BSII-B-010.PNG) menu item.
* The new Sign In screen is displayed
  > ![B S I I B 020](BlazorServerIdentityInterop/wwwroot/images/BSII-B-020.PNG)
* Proceed to Sign In
* If successful, the top line menu display will change.  
* But this time the UI is all Blazor, and should proceed smoothly.

### Commit to Repo Base 00.03.01
#### Sign Out
* Create a Blazor component called *SignOut.razor*
* This is pretty much like *SignIn.razor*, except no UI, and call Logout instead.
     ```c#
    @page "/SignOut"

    @using System.Threading.Tasks;
    @using Microsoft.AspNetCore.Antiforgery;
    @inject IJSRuntime _jsruntime;

    @code {
        //Will tell us if signed in or nor
        [CascadingParameter] public Task<AuthenticationState> authenticationStateTask { get; set; }

        // local property for the AF
        private string AntiForgeryToken { get; set; }

        // Execute after UI has rendered.  This allows it to fire once and only once.
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            var authState = await authenticationStateTask;

            if (firstRender && authState.User.Identity.IsAuthenticated)
            {
                // Get the AF token
                var interop = new Interop(_jsruntime);
                AntiForgeryToken = await interop.GetElementByName("__RequestVerificationToken");

                // Build a form list
                var fields = new
                {
                    __RequestVerificationToken = AntiForgeryToken
                    , returnurl = "/SignOut"
                };

                // Post the message
                await interop.SubmitForm("Identity/Account/Logout", fields);
            }

            await base.OnAfterRenderAsync(firstRender);
            return;
        }
    }

    ```
#### Test
* Use `Sign In`
* Use `Sign Out`
#### Optional Put out a Message
* Add a little text once Signed Out
    ```
    @inject IJSRuntime _jsruntime;

    <AuthorizeView>
        <NotAuthorized>
            <h2 class="text-center">You are signed out</h2>
        </NotAuthorized>
    </AuthorizeView>

    @code {

    ```

### Commit to Repo Base 00.03.02

#### The Rest
`SignInManager` does *NOT* work in Blazor.  
`UserManager` works, but is not supported.  
This project is about getting Blazor working with MS Identity for Educational, POC, or other non-Productional Quality activity, we can live with using `UserManager` for the rest of the Identity pages, unless `SignInManager` is involved.
We can build the rest by merger the Razor pages into Blazor Components.

#### SignUp.razor
This is the replacement for the Registration set of pages.
* Create the file
* Copy the contents of *Register.cshtml* into the file
* Copy the contents of *Register.cshtml.cs* into the file.

#### Commit SignUp.razor
For educational purpose, saved *SignUp.razor* in original state to see the transformation.

#### Converting to a Blazor Component
* The general flow is to move blocks of code around and transform the form into an EditForm
* Put a page name on
* Moved all the libraries to the top
* Surrounded the code into a @code{} block
* Removed the namespace
* Removed the PageModel
* Commit

#### Convert to an EditForm
* EditForm are new for Blazor.
* Make the changes
* Remove `Model` from the External Logins
* Commit

#### Convert the @code\{\} block
* Convert all the DI properties into @inject
* Right click on the page and use `Format Selection` to straighten out indention.
* Remove remaining DI material in constructor.
* The `[BindProperty]` attribute is unnecessary.
* Comment out `OnGetOnAsync()` method
* Convert
`    public async Task<IActionResult> OnPostAsync(string returnUrl = null)`
to 
`    public async void ValidSubmit()`
* The logic gets inverted from the template, which is if successful, continue.
The new logic is If error, exit, otherwise continue.
* See *SignUp.razor* for remaining details

#### Notes
* The Model designation in Editform should be without an @  
`<EditForm Model="Input" OnValidSubmit="@ValidSubmit" OnInvalidSubmit="@InvalidSubmit">`  
Otherwise, an error message about "...can be Model or EditContext, but not both" appears
* *RegisterConfirmation.cshtml* usually handles displaying the confirmation.  That has been absorbed into this SignUp.
* A target for the confirmation needs to be specified.  That will be *SignUpConfirmEmail.razor**
* QueryHelpers is a Microsoft extension for building URLs with query string parameters.  Very helpful.
* Need better error reporting for condistions of Form=ok, but processing error.  For example, enrolling the same email twice. 
The solution I use involves a `ServerSideValidator` and multiple lists inside a dictionary.  Got to be a simpler way..

#### NavigationManagerExtensions
Extensions provide a way to extend a class, especially a sealed one like NavigationManager.
Chris Sainty has provided a neat extension around querystrings and NavigationManager.  It has been added the Extensions folder.

#### SignUpConfirmEmail.razor
* Implement the page from the repo
* The `<SignIn />` component has been embedded.  User can't sign in until email confirmation.  
Now that it is confirmed upon hitting this page, present SignIn so that they can.
* Thanks to Chris Sainty and his blog for info about query strings (https://chrissainty.com/working-with-query-strings-in-blazor/)
* 
#### Test it
* Sign up a new user
* Confirm their email
* Sign In

### Commit to Repo Base 00.03.03
* Completed transformation of *SignUp.razor*
  * Kept Confirmation generation inside.  No conversion of *RegisterConfirmation* necessary.
* Added *SignUpConfirmEmail.razor* as a target for confirmation url.
* Added *NavigationManagerExtensions.cs* for an extension class to Navigation Manager

### Commit to Repo Base 00.03.04
* Lined up the box for SignUp
* Added SignalR to package for Azure dependency.

### Commit to Repo Base 00.03.05
* Some beautification
* Added self-migrate for SqlIte
* Updated all packages.

### Commit Base 00.03.06
* Put SignIn `<EditForm >` inside a '`<AuthorizeView>` so it is only displayed when unauthed.
* Utilized EditContext to test and report server errors.  It is good, but after displaying an error, 
it is stuck on the error.  Had to reset the EditContext inside `InvalidSubmit` sinc that seems to be the only thing active.
* Added ClearErrors button and method to reinit EditContext.  Must be an eventhandler somewhere to tap into.  But, you know... Docs!

### Commit Base 00.03.07
* Added *ServerSideValidator* component to process server validation.  
Inside of `OnValidSubmit()` tests can be run, and if failed, messages placed inside `ServerSideValidator()` for eventual display.
This pattern doesn't suffer from the problems in `SignIn()` No need for a *Clear Error* button.
One little bit of wonkyness is figuring out which field had the problem.
* Cleaned up the UI a little to make it presentable on a mobile.

## Base 00.04.00 A Big Tweak
### Notes
I've been working on a different approach using the `RevalidatingIdentityAuthenticationSerivceProvider` class  that comes will you add add Identity during project creation creation.
(See project BlazorIdentityRIASP) It works, just like every other solution right up until getting the Identity cookie.
There just really isnt' getting around using SignInManager to populate HttpContext for MS Identity to work.
But HttpContext is frozen in Blazor and unmodifyable.  
So I started doing some experimenting with PostMan calling Login, against this project, and it work.
Most importantly the .AspNetCore.Identity.Application cookie was available.
Further, I found some SO answers showing how to generate a cookie during Startup.  
Lastly, some Microsoft docs referencing if you really must use HttpContext, access it in the startup `_Host.cshtml` file and cascde it down through Blazor.

So I did the following:
* Changed `Startup.cs` to define the anti-forgery cookie, then accessed it in my code.  This went well.
* Changed _Host.cshtml to save the anti-forger cookie ito the DOM in a diffent way.
* Made calls to Identity/Account/Login passing the token.  This didn't go so well, kept missing the XSRF-TOKEN header.
* I eventually found RestSharp, and it made the creating the request much easier.
  * I read a lot of debate about the pros and cons of RestSharp.
    My interest is Blazor Local Identity without resorting to Razor Pages, bolting 3rd party products on system, and having UX as good as Asp.Net MVC Identity.
    A bit obsessive, but I have my reasons.  RestSharp solved a problem to help me get to my goal.  
    It is an issue to mopped up at a later date.
* And then I figured out how to save the authentication cookie into the Browser.
  * I asked a lot of people about how to do this, as this was a problem with most solutions, and no good answers.
  * Turns out the file `interop.js` from Oqtane had the solution all along `setcookie()`.
* Finally, I always knew that the anti-forgery token came on the initial GET for the Login form, in a hidden field.
  But using Fiddler (Oh how I miss my days of WireSharking wire protocols! Programming in Lua.  Yes!),
  to watch a Razor page Login cycle, I could see the same value was in the XSRF-TOKEN header in the GET response.
  * I built a GET against the Login Razor page, pulled out the XSRF-TOKEN, and then used that in my POST to Login with credentials.
  * This allowed me to comply with anti-forgery, pass credentials, get the authentication token back, and insert into the browser storage.
* This version contains all the prior work, plus all the new work.  Then in the next version, I'll cut things back to just the working code.

* *Startup.cs*
    * Added a bunch of packages.  I'll prune all this back next tag
    * Defined my AntiForgery Header and Form Field names
    * Added HttpClient
    * Append the XSRF-TOKEN to HttpContext BEFORE Blazor space.  It is mutable at this point
* *appsettings.json* *appsettings.Development.json*
  * Set everything to `Trace`  This was a big help when trying to identify what was going wrong where.
  Leaving this in just for reference purposes.
* *_Host.cshtml*  _Tomayto_ or _Tomotto__
  * In the Oqtane solution, `HTML.AntiForgeryToken()` is called to insert a token element that can be retrieved later.
  * I found MS guidance that for Blazor, you should inject the IAntiforgery service, create a function to retrieve a token and then set it to a hidden field.
  * I've got both approaches going, and while they produce different values for the token, they both seem to work.  I can't see if one is superior the other besides brevity.
* *Index.razor*
  * Added an extra page attribute for `/Index` as the system was having problems with `/`
*BlazorServerIdentityInterop.csproj*
  * **Optional** - Added the RestSharp package.  If you can call Razor Login using HttpClient, great.
  * I put the RetSharp compiler directive around the code to easily turn it off and on.
*SignIn.razor*
    * Added a bunch of new packages.  Will prune back later.
    * Added a HttpClientFactory
    * Added token for the Oqtane token, and xsrfcookie for XSRF-TOKEN.
    * Added xsrf to InputModel to serialize the whole structure.
    * Added property OnGetResponse to hold the respone to an HttpClient call
    * Commented out retrieval of the Oqtane token by element name.
    * Compiler directives to isolate the Oqtane/Interop & RestSharp code from everythign else.
    * Added a RestSharp section.
      * Originally was getting the anti-forgery token from DOM with GetElementByName.  
        But with better understanding, I realized what I needed was the Login form from a GET call.
        It had everything I needed in the right form.  I switched over to the new code where I populate the postRequest with data from the getResponse.
      * Then there is a postRequest sent.
        * From the response, I extract the `.AspNetCore.Identity.Application` cookie and then add it the DOM.
          * I use the SetCookie method provided by the `interop.js*  This reduces my javascript calls to 1.
      * `NavigateTo("/Index", true)` is crucial to force the browser to route through the server and pick up the authentication cookie.
        Without this step, the browser will never reflect the user is authenticated. 
*serviceDependencies.json* *serviceDepenencies.local.json*
* Didn't notice these until now.  I suspect RestSharp added them.  Don't know.

### Base 00.04.01 Reduction
*Eliminating all the files not needed with the current approach
*Adding all the files that would be present without scaffolding

### Base 00.04.02 Log.md
Moved the logs out of Readme.md so it can be big picture and this little picture

### Base 00.04.03 SUSO changes
#### Functional changes
##### SignUp Flow
###### SignUp.razor
* Markup file
###### SignUp.razor.cs
* Code behind file
###### SignUpEmailConfirmed.razor
* Called by confirmation link and confirms an email address
##### SignIn.razor
* Removed deprecated code
* Added commentary
##### Startup.cs
* Added new packages
* Removed deprecated
* Added DBContext to match with template project
* Added ASP
* Removed creating serviceScope
##### BlazorServerIdentityInterop.csproj
* Removed deprecated functionality

#### UI Changes
##### Index.razor
* Added an AuthorizeView to demonstrate A&A working
##### LoginDisplay.razor
* Tweaked the Sign Out display link
##### MainLayout.razor
* Tweaked display

#### Clean up
##### _Host.cshtml
* Removed all code that inserts AF token into DOM.  Not necessary if getting the AF token from OnGet() 
##### interop.js & interop.cs
* Reduced to minimum required
##### ServerSideValidator.cs
* Moved from Identity to mainline to make available anywhere
##### NavigationManagerExtensions & IdentityExtensions.cs
* Replaced with IdentityExentions.cs 

#### Miscellaneous
##### App.razor
* Changed to `AuthorizeRouteView`
##### _Imports.razor
* Added Components folder
##### appsettings.json
* Changed the DBConnect to the template version
* Turned off Console logging
##### serviceDependencies.json & local.json
* Switched over to mssql1
* 
#### Documentation
##### Images
* Added new images to document the replication steps.
##### Log.md
* Base 00.04.03

## V02.00.00 

### V02.00.01
* Ran into problem after published to Azure, added Microsoft.Extensions.Logging.AzureAppServices to get logging.
* Logs inidcated database hadn't been updated.
* Needed a local connection string to run migratio against Azure Sql database.
* Once I got through that, a firewall prevented connectivity.
* Once FW configured, connected to database, performed migration and application launched.
* Success.
* Put the Azure connection string in Secrets.json, and remove from appsettings.json.
  Testing successful.
#### appsettings.json
* Webdeploy added a section on Azure
#### serviceDependencies.json
* Added by webdeploy
#### serviceDependecies.json & serviceDependencies.local.json
* Webdeploy added more nodes
#### BlazorServerIdentityInterop.csproj
* SignalR and AzureAppServices logging added.
#### Program.cs
Added AddAzureWebApplicationDiagnostics
