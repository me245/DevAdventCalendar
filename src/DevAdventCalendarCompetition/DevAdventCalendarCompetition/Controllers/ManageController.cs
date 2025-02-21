﻿using DevAdventCalendarCompetition.Extensions;
using DevAdventCalendarCompetition.Models.ManageViewModels;
using DevAdventCalendarCompetition.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace DevAdventCalendarCompetition.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class ManageController : Controller
    {
        private readonly IManageService _manageService;
        private readonly IAccountService _accountService;
        private readonly ILogger _logger;
        private readonly UrlEncoder _urlEncoder;

        private const string AuthenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";
        private const string RecoveryCodesKey = nameof(RecoveryCodesKey);

        public ManageController(IManageService manageService, IAccountService accountService, ILogger<ManageController> logger, UrlEncoder urlEncoder)
        {
            _manageService = manageService;
            _accountService = accountService;
            _logger = logger;
            _urlEncoder = urlEncoder;
        }

        [TempData]
        public string StatusMessage { get; set; }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _manageService.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Nie można załadować użytkownika z identyfikatorem '{_accountService.GetUserId(User)}'.");
            }

            var model = new IndexViewModel
            {
                Username = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                IsEmailConfirmed = user.EmailConfirmed,
                StatusMessage = StatusMessage
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(IndexViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _manageService.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Nie można załadować użytkownika z identyfikatorem '{_accountService.GetUserId(User)}'.");
            }

            var email = user.Email;
            if (model.Email != email)
            {
                var setEmailResult = await _manageService.SetEmailAsync(user, model.Email);
                if (!setEmailResult.Succeeded)
                {
                    throw new ApplicationException($"Wystąpił nieoczekiwany błąd podczas konfigurowania wiadomości e-mail dla użytkownika z identyfikatorem '{user.Id}'.");
                }
            }

            var phoneNumber = user.PhoneNumber;
            if (model.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _manageService.SetPhoneNumberAsync(user, model.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    throw new ApplicationException($"Wystąpił nieoczekiwany błąd podczas ustawiania numeru telefonu dla użytkownika z identyfikatorem '{user.Id}'.");
                }
            }

            StatusMessage = "Profil został zaktualizowany";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendVerificationEmail(IndexViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _manageService.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Nie można załadować użytkownika z identyfikatorem '{_accountService.GetUserId(User)}'.");
            }

            var code = await _accountService.GenerateEmailConfirmationTokenAsync(User);
            var callbackUrl = Url.EmailConfirmationLink(user.Id, code, Request.Scheme);
            var email = user.Email;
            await _accountService.SendEmailConfirmationAsync(email, callbackUrl);

            StatusMessage = "E-mail weryfikacyjny został wysłany. Sprawdź swoją skrzynkę odbiorczą";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> ChangePassword()
        {
            var user = await _manageService.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Nie można załadować użytkownika z identyfikatorem '{_accountService.GetUserId(User)}'.");
            }

            var hasPassword = await _manageService.HasPasswordAsync(user);
            if (!hasPassword)
            {
                return RedirectToAction(nameof(SetPassword));
            }

            var model = new ChangePasswordViewModel { StatusMessage = StatusMessage };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _manageService.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Nie można załadować użytkownika z identyfikatorem '{_accountService.GetUserId(User)}'.");
            }

            var changePasswordResult = await _manageService.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                AddErrors(changePasswordResult);
                return View(model);
            }

            await _accountService.SignInAsync(user);
            _logger.LogInformation("User changed their password successfully.");
            StatusMessage = "Twoje hasło zostało zmienione";

            return RedirectToAction(nameof(ChangePassword));
        }

        [HttpGet]
        public async Task<IActionResult> SetPassword()
        {
            var user = await _manageService.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Nie można załadować użytkownika z identyfikatorem '{_accountService.GetUserId(User)}'.");
            }

            var hasPassword = await _manageService.HasPasswordAsync(user);

            if (hasPassword)
            {
                return RedirectToAction(nameof(ChangePassword));
            }

            var model = new SetPasswordViewModel { StatusMessage = StatusMessage };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPassword(SetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _manageService.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Nie można załadować użytkownika z identyfikatorem '{_accountService.GetUserId(User)}'.");
            }

            var addPasswordResult = await _manageService.AddPasswordAsync(user, model.NewPassword);
            if (!addPasswordResult.Succeeded)
            {
                AddErrors(addPasswordResult);
                return View(model);
            }

            await _accountService.SignInAsync(user);
            StatusMessage = "Your password has been set.";

            return RedirectToAction(nameof(SetPassword));
        }

        #region Helpers

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        //TODO: move to service
        private string FormatKey(string unformattedKey)
        {
            var result = new StringBuilder();
            int currentPosition = 0;
            while (currentPosition + 4 < unformattedKey.Length)
            {
                result.Append(unformattedKey.Substring(currentPosition, 4)).Append(" ");
                currentPosition += 4;
            }
            if (currentPosition < unformattedKey.Length)
            {
                result.Append(unformattedKey.Substring(currentPosition));
            }

            return result.ToString().ToLowerInvariant();
        }
       
        #endregion Helpers
    }
}