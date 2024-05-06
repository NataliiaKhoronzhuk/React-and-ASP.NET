using System.Net.Mail;
using AvantiPoint.EmailService;
using Microsoft.EntityFrameworkCore;
using MultiFamilyPortal.Data;
using MultiFamilyPortal.Data.Models;
using MultiFamilyPortal.Dtos;
using MultiFamilyPortal.Services;

namespace MultiFamilyPortal.Apis;


public static class Forms
{
    public static WebApplication MapForms(this WebApplication app)
    {
        var formGroup = app.MapGroup("/api/forms");
        formGroup.MapGet("/contact-us", ContactUs);
        formGroup.MapGet("/investor-inquiry", InvestorInquiry);
        formGroup.MapGet("/newsletter-subscriber", NewsletterSubscriber);

        return app;
    }

    public static async ValueTask<IResult> ContactUs(IEmailService _emailService, IEmailValidationService _emailValidator, ISiteInfo _siteInfo, ITemplateProvider _templateProvider, ContactFormRequest form, HttpRequest request)
    {
        var validatorResponse = _emailValidator.Validate(form.Email);

        if (!validatorResponse.IsValid)
        {
            return Results.BadRequest(new FormResult
            {
                Errors = new Dictionary<string, List<string>>
                    {
                        { nameof(ContactFormRequest.Email), new List<string> { validatorResponse.Message } }
                    },
                Message = validatorResponse.Message,
                State = ResultState.Error
            });
        }

        var url = $"{request.Scheme}://{request.Host}";

        var notification = new ContactNotificationTemplate
        {
            Comments = form.Comments,
            Email = form.Email,
            FirstName = form.FirstName,
            LastName = form.LastName,
            Phone = form.Phone,
            Subject = "Contact Form Request"
        };
        var notificationMessage = await _templateProvider.GetTemplate(PortalTemplate.ContactNotification, notification);
        await _emailService.SendAsync(notificationMessage);

        var userNotification = new ContactFormEmailNotification
        {
            DisplayName = $"{form.FirstName} {form.LastName}".Trim(),
            Email = form.Email,
            FirstName = form.FirstName,
            LastName = form.LastName,
            Message = $"<p>Thank you for contacting us. One of our team members will be in touch shortly.</p>",
            SiteTitle = _siteInfo.Title,
            SiteUrl = url,
            Subject = $"Investor Request {_siteInfo.Title}",
            Year = DateTime.Now.Year
        };
        var userMessage = await _templateProvider.GetTemplate(PortalTemplate.ContactMessage, userNotification);
        var emailAddress = new MailAddress(form.Email, $"{form.FirstName} {form.LastName}".Trim());
        await _emailService.SendAsync(emailAddress, userMessage);

        return Results.Ok(new FormResult
        {
            Message = "Success! Contact Request was successfully sent.",
            State = ResultState.Success
        });
    }

    public static async ValueTask<IResult> InvestorInquiry(IMFPContext _dbContext, IEmailService _emailService, IEmailValidationService _emailValidator, ITemplateProvider _templateProvider, ISiteInfo _siteInfo, InvestorInquiryRequest form, HttpRequest request)
    {
        var validatorResponse = _emailValidator.Validate(form.Email);

        if (!validatorResponse.IsValid || form.LookingToInvest is null)
        {
            return Results.BadRequest(new FormResult
            {
                Errors = new Dictionary<string, List<string>>
                    {
                        { nameof(ContactFormRequest.Email), new List<string> { validatorResponse.Message } }
                    },
                Message = validatorResponse.Message,
                State = ResultState.Error
            });
        }

        await _dbContext.InvestorProspects.AddAsync(new InvestorProspect
        {
            Email = form.Email,
            FirstName = form.FirstName,
            LastName = form.LastName,
            LookingToInvest = form.LookingToInvest.Value,
            Phone = form.Phone,
            //Comments = form.Comments,
            Timezone = form.Timezone,
            Comments = form.Comments,
        });

        await _dbContext.SaveChangesAsync();

        var url = $"{request.Scheme}://{request.Host}";
        var notification = new InvestorInquiryNotificationTemplate
        {
            Comments = form.Comments,
            Email = form.Email,
            FirstName = form.FirstName,
            LastName = form.LastName,
            Phone = form.Phone,
            LookingToInvest = form.LookingToInvest.Value.ToString("C"),
            Timezone = form.Timezone,
            Subject = $"Investor Inquiry - {form.FirstName} {form.LastName}"
        };
        var notificationMessage = await _templateProvider.GetTemplate(PortalTemplate.InvestorNotification, notification);
        await _emailService.SendAsync(notificationMessage);

        var investorInquiryNotification = new ContactFormEmailNotification
        {
            DisplayName = $"{form.FirstName} {form.LastName}".Trim(),
            Email = form.Email,
            FirstName = form.FirstName,
            LastName = form.LastName,
            Message = $"<p>Thank you for contacting us. One of our team members will be in touch shortly.</p>",
            SiteTitle = _siteInfo.Title,
            SiteUrl = url,
            Subject = $"Investor Request {_siteInfo.Title}",
            Year = DateTime.Now.Year
        };
        var investorMessage = await _templateProvider.GetTemplate(PortalTemplate.ContactMessage, investorInquiryNotification);
        var emailAddress = new MailAddress(form.Email, $"{form.FirstName} {form.LastName}".Trim());
        await _emailService.SendAsync(emailAddress, investorMessage);

        return Results.Ok(new FormResult
        {
            Message = "Success! Investor Inquiry was successfully sent. A member of our team will be in touch shortly!",
            State = ResultState.Success
        });
    }

    public static async ValueTask<IResult> NewsletterSubscriber(IMFPContext _dbContext, IEmailService _emailService, IEmailValidationService _emailValidator, ITemplateProvider _templateProvider, ISiteInfo _siteInfo, IIpLookupService _ipLookup, HttpRequest request, NewsletterSubscriberRequest form, IHttpContextAccessor httpContextAccessor)
    {
        var validatorResponse = _emailValidator.Validate(form.Email);

        if (!validatorResponse.IsValid)
        {
            return Results.BadRequest(new FormResult
            {
                Errors = new Dictionary<string, List<string>>
                    {
                        { nameof(ContactFormRequest.Email), new List<string> { validatorResponse.Message } }
                    },
                Message = validatorResponse.Message,
                State = ResultState.Error
            });
        }

        var blogContext = _dbContext as IBlogContext;
        if (await blogContext.Subscribers.AnyAsync(x => x.Email == form.Email))
        {
            return Results.Ok(new FormResult
            {
                Message = "You have already subscribed.",
                State = ResultState.Success
            });
        }

        var ipData = await _ipLookup.LookupAsync(httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress, request.Host.Value);

        var subscriber = new Subscriber
        {
            City = ipData.City,
            Continent = ipData.Continent,
            Email = form.Email,
            IpAddress = httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress,
            Country = ipData.Country,
            Region = ipData.Region,
        };
        blogContext.Subscribers.Add(subscriber);
        await blogContext.SaveChangesAsync();

        var url = $"{request.Scheme}://{request.Host}";
        var confirmationUrl = $"{url}/subscriber/confirmation/{subscriber.ConfirmationCode}";

        var notification = new ContactFormEmailNotification
        {
            DisplayName = form.Email,
            Email = form.Email,
            FirstName = form.Email,
            LastName = form.Email,
            Message = $"<p>Thank you for signing up!<br />Before we start sending you messages, please confirm that this email address belongs to you and that you would like to recieve messages from us. Don't worry, if you didn't sign up, you won't get anything from us unless your confirm you email address.</p><div class=\"text-center\"><p><a href=\"{confirmationUrl}\">Confirm Email</a><br />Link not working? Copy and paste this url into your browser: {confirmationUrl}</p>",
            SiteTitle = _siteInfo.Title,
            SiteUrl = url,
            Subject = $"Successfully subscribed to updates on {_siteInfo.Title}",
            Year = DateTime.Now.Year
        };
        var message = await _templateProvider.GetTemplate(PortalTemplate.ContactMessage, notification);
        await _emailService.SendAsync(form.Email, message);

        return Results.Ok(new FormResult
        {
            Message = "Success! You have succesfully subscribed to our updates.",
            State = ResultState.Success
        });
    }
}