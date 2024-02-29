# SqlMembershipAdapter

Adapter for legacy Microsoft ASP.NET Membership tables without the need for Web configuration.

Continuing to use these tables is strongly discouraged - you may use this library to at least remove a direct .NET Framework dependency, however you should consider the security implications of continuing to use legacy Membership tables (see Disclaimer).

# Usage

The original Microsoft ASP.NET Membership implementation requires .NET Framework and is tied to Web configuration and native Windows binaries for encryption. The classic Membership tables are considered legacy and ASP.NET Identity is encouraged by Microsoft as its replacement - the original SHA1/MD5 hash functions it supports are also compromised, so should be avoided.

If you need to continue use of legacy Membership tables but wish to move away from .NET Framework, you may use this adapter as a temporary means of interaction while migrating to a secure implementation.

# Examples

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

For this library, the models are replaced with request and response models, rather than lots of `ref` and `out` params like in the original implementation - the above example would therefore look like this:

```
SqlMembershipService membership = new(new SqlMembershipSettings()
{
    MinRequiredNonAlphanumericCharacters = 1
});

CreateUserResponse response = membership.CreateUser(new CreateUserRequest()
{
    Email = "email@example.com",
    Password = "hunter2"
});
```

You will need to adapt your Membership code throughout to send and receive these models, however the same data will ultimately be returned.

# How it was created

This library is based on the SQL interaction in the Microsoft ASP.NET Membership [source code](https://github.com/microsoft/referencesource/tree/master/System.Web.ApplicationServices) which has been made available to the public - the original implementation  is closely linked to Windows and the `System.Web.ApplicationServices` web config serialization.

# Removed features

- Machine key and web config validation.
- Exception types and messages will be close to the `SQLMembershipProvider` validation and exceptions, however the full range such as provider key validation from text web-config translation will not be raised.
- Password decoding is removed - this was tied to `UnsafeNativeMethods` which required Windows native binaries, such as `GetSHA1Hash`.
- Database schema validation
- Performance Counters (XML)
- HttpException references

# Disclaimer

By using this library, you acknowledge and accept that the responsibility for any issues, including but not limited to security vulnerabilities, data breaches, or system failures, rests solely with you as the user. The author of this library cannot be held liable for any damages or consequences arising from its use.

It's crucial to understand that the Microsoft Membership system, especially in legacy setups, might not meet modern security standards. While this library facilitates interfacing with these systems for compatibility reasons, it's essential to acknowledge the potential risks associated with using these systems.

The author provides this library as-is, without any warranties or guarantees of its fitness for a particular purpose. It's your responsibility to assess the suitability of this library for your use case and to implement additional security measures as necessary.
