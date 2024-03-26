# SqlMembershipAdapter

Adapter for legacy Microsoft ASP.NET Membership tables without the need for Web configuration.

Continuing to use these tables is discouraged on modern systems - you may use this library to at least remove a direct .NET Framework dependency or adapt to your own needs, however you should consider the security risks of not migrating.

[![nuget](https://badgen.net/nuget/v/SqlMembershipAdapter?icon=nuget)](https://www.nuget.org/packages/SqlMembershipAdapter)

# Usage

The original Microsoft ASP.NET Membership implementation requires .NET Framework and is tied to Web configuration and native Windows binaries for encryption. The classic Membership tables are considered legacy and ASP.NET Identity is encouraged by Microsoft as its replacement - the original SHA1/MD5 hash functions it supports are also compromised, so should be avoided.

If you need to continue use of legacy Membership tables but wish to move away from .NET Framework, you may use this adapter as a temporary means of interaction while migrating to a secure implementation.

# Examples

## MembershipProvider

Here is an example of creating a user using the original .NET Framework Membership:

```
Membership.CreateUser(username, password, email, null, null, true, out status);
```

You would have an associated Web configuration, looking something like this:

```
<membership defaultProvider="AspNetSqlMembershipProvider">
  <providers>
    <clear />
    <add name="AspNetSqlMembershipProvider" type="System.Web.Security.SqlMembershipProvider" connectionStringName="MembershipConnection" applicationName="/" />
  </providers>
  <passwordSettings>
    <add name="minRequiredNonAlphanumericCharacters" value="1" />
  </passwordSettings>
</membership>
```

For this library, the models are replaced with request and response models, rather than lots of `ref` and `out` params like in the original implementation - the above two features would therefore be condensed to:

```
SqlMembershipClient membership = new(new SqlMembershipSettings("connectionString")
{
    MinRequiredNonAlphanumericCharacters = 1
});

CreateUserResult createUserResult = await _sut.CreateUser(new CreateUserRequest(
	username, password, email, passwordQuestion, passwordAnswer, isApproved, userId));
```

You will need to adapt your Membership code throughout to send and receive these models, however the same data will ultimately be returned and relevant exceptions thrown.

You may want to use `ISqlMembershipClient` within a Dependency Injection container to re-use the `SqlMembershipClient` instance and its configuration for other calls.

## RoleProvider

Under the same install as the Membership tables is the Role functionality - this has been added to the Membership adapter as `SqlRoleClient`.

Originally you would configure the Provider alongside Membership in your web config:

```
<roleManager enabled="true" defaultProvider="SqlRoleProvider">
    <providers>
        <clear />
        <add name="SqlRoleProvider" type="System.Web.Security.SqlRoleProvider" connectionStringName="MembershipConnection" applicationName="/" />
    </providers>
</roleManager>
```

Then you would identify the provider and add users to roles:

```
SqlRoleProvider roleProvider = (SqlRoleProvider)Roles.Provider;

string[] usernames = { "user1", "user2" };
string[] roleNames = { "role1", "role2" };

roleProvider.AddUsersToRoles(usernames, roleNames);
```

The `SqlRoleClient` adapter uses identical param types:

```
SqlRoleClient roleClient = new("connectionString");

string[] usernames = { "user1", "user2" };
string[] roleNames = { "role1", "role2" };

roleClient.AddUsersToRoles(usernames, roleNames);
```

# How it was created

This library is based on the SQL interaction in the Microsoft ASP.NET Membership [source code](https://github.com/microsoft/referencesource/tree/master/System.Web.ApplicationServices) which has been made available to the public - the original implementation is closely linked to Windows and the `System.Web.ApplicationServices` web config serialization.

The code attempts to retain identical conditions for exception, code flow, and try/finally blocks, with some adjustments throughout to enable unit testing and removing web config file references.

# Removed features

- Machine key and web config validation.
- Exception types and messages will be close to the `SQLMembershipProvider` validation and exceptions, however the full range such as provider key validation from text web-config translation will not get thrown
- Password decoding and keyed hash options is removed - this was tied to `UnsafeNativeMethods.cs` which required Windows native binaries, such as `GetSHA1Hash`
- Static references - the database calls are asynchronous
- Membership schema validation
- Performance Counters (XML)
- HttpException references

# Disclaimer

By using this library, you acknowledge and accept that the responsibility for any issues, including but not limited to security vulnerabilities, data breaches, or system failures, rests solely with you as the user. The author of this library cannot be held liable for any damages or consequences arising from its use.

It's crucial to understand that the Microsoft Membership system, especially in legacy setups, might not meet modern security standards. While this library facilitates interfacing with these systems for compatibility reasons, it's essential to acknowledge the potential risks associated with using these systems.

The author provides this library as-is, without any warranties or guarantees of its fitness for a particular purpose. It's your responsibility to assess the suitability of this library for your use case and to implement additional security measures as necessary.
