using System.Text;
using Microsoft.EntityFrameworkCore;
using MultiFamilyPortal.Data;
using MultiFamilyPortal.Data.Internals;
using MultiFamilyPortal.Dtos;
using MultiFamilyPortal.Services;
using vCardLib.Enums;
using vCardLib.Models;
using vCardLib.Serialization;

namespace MultiFamilyPortal.Apis;

public static class About
{
    public static WebApplication MapAbout(this WebApplication app)
    {
        var aboutGroup = app.MapGroup("/api/about");
        aboutGroup.MapGet("/profile/{firsName}/{lastName}", GetUserProfile);
        aboutGroup.MapGet("/profile/vcard/{userId}", DownloadVCard);

        return app;
    }

    //GetHightedUsers method

    public static async ValueTask<IResult> GetHighlightedUsers(IMFPContext _dbContext)
    {
        var users = await _dbContext.HighlightedUsers
                .Include(x => x.User)
                    .ThenInclude(x => x.SocialLinks)
                        .ThenInclude(x => x.SocialProvider)
                .ToListAsync();

        var response = users.OrderBy(x => x.Order)
            .Select(x => new HighlightedUserResponse
            {
                Bio = x.User.Bio,
                DisplayName = x.User.DisplayName,
                Email = x.User.Email,
                Phone = x.User.PhoneNumber,
                Links = x.User.SocialLinks.Select(s => new SocialLinkResponse
                {
                    Icon = s.SocialProvider.Icon,
                    Name = s.SocialProvider.Name,
                    Link = s.Uri.ToString()
                }),
                Title = x.User.Title,
            });

        return Results.Ok(response);
    }

    //GetUserProfile method

    public static async ValueTask<IResult> GetUserProfile(IBaseContext _baseContext, string firstName, string lastName)
    {
        var user = await _baseContext.Users
                .Include(x => x.SocialLinks)
                    .ThenInclude(x => x.SocialProvider)
                .FirstOrDefaultAsync(x => x.FirstName == firstName && x.LastName == lastName);

        if (user is null)
            return Results.NotFound();

        var response = new HighlightedUserResponse
        {
            Id = user.Id,
            Bio = user.Bio,
            DisplayName = user.DisplayName,
            Email = user.Email,
            Links = user.SocialLinks
                .Select(x => new SocialLinkResponse
                {
                    Icon = x.SocialProvider.Icon,
                    Link = x.Uri.ToString(),
                    Name = x.SocialProvider.Name,
                }),
            Phone = user.PhoneNumber
        };

        return Results.Ok(response);
    }

    //DownloadVCard method

    public static async ValueTask<IResult> DownloadVCard(IBaseContext _baseContext, string userId, IIpLookupService _ipLookup, HttpContext httpContext)
    {
        var profile = await _baseContext.Users
                .Include(x => x.SocialLinks)
                    .ThenInclude(x => x.SocialProvider)
                .FirstOrDefaultAsync(x => x.Id == userId);

        if (profile is null)
            return Results.Redirect("/not-found");

        var ipData = await _ipLookup.LookupAsync(httpContext.Connection.RemoteIpAddress, httpContext.Request.Host.Value);

        var card = new vCard(vCardVersion.v3)
        {
            
            Organization = new Organization { Name= _baseContext.GetSetting<string>(PortalSetting.LegalBusinessName) },
            Title = profile.Title,
            Kind = ContactKind.Individual,
            Language = new Language { Locale = "en-US" },
            EmailAddresses = new List<EmailAddress>
            {
                new EmailAddress { Value = profile.Email, Type = EmailAddressType.Work }
            },

            Name = new Name { GivenName = profile.FirstName, FamilyName = profile.LastName },
            
           
            Note = @$"Contact added {DateTime.Now:D}
                    Added from {ipData.Ip}
                    On or near: {ipData.City}, {ipData.Region}",
           
            Photos = new List<Photo>
                {
                    new Photo
                    {
                        Encoding = "BASE64",
                        Type = "b",
                        Value = GravatarHelper.GetUri(profile.Email, 180, DefaultGravatar.MysteryPerson).ToString()
                    }
                },
            CustomFields = new List<KeyValuePair<string, string>>()
        };

        foreach (var link in profile.SocialLinks)
        {
            var key = $"socialProfile;type={link.SocialProvider.Name.ToLower()}";
            var uri = string.Format(link.SocialProvider.UriTemplate, link.Value);
            card.CustomFields.Add(new KeyValuePair<string, string>(key, uri));
        }

        var fileName = $"{profile.FirstName}{profile.LastName}.vcf".Replace(" ", "").Trim();
        var data = Encoding.Default.GetBytes(vCardSerializer.Serialize(card));
        return Results.File(data, "text/x-vcard", fileName);
    }
}
