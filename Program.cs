﻿using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace hometax_login
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            // PowerShell 및 Playwright 설치 확인 및 설치
            if (!CheckIfPowerShellInstalled())
            {
                InstallPowerShell();
            }
            else
            {
                Console.WriteLine("PowerShell이 이미 설치되어 있습니다.");
            }
            InstallPlaywrightBrowsers();


            // Playwright를 사용하여 Chromium 실행 및 설정
            string userHomeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string playwrightInstallScript = Path.Combine(userHomeDirectory, "Desktop", "Medi_Web2", "ms-playwright","chromium-1117", "chrome-win", "chrome.exe");
            Console.WriteLine($"Chromium 실행 파일 경로: {playwrightInstallScript}");
            using var playwright = await Playwright.CreateAsync();
            var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = false,
                Args = new[] { "--remote-debugging-port=9222" },
                ExecutablePath = playwrightInstallScript
            });
            Console.WriteLine("브라우저가 성공적으로 실행되었습니다.");
            var context = await browser.NewContextAsync();
            var page = await context.NewPageAsync();

            //명령줄 인수 검사 코
            if(args.Length < 2)
            {
            Console.WriteLine("args error");
            return;
            }
            string title = args[0];
            string password = args[1];
            
            // 요양기관정보마당 홈페이지 접속
            await page.GotoAsync("https://medicare.nhis.or.kr/portal/index.do");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle); 
            // 로그인 버튼 클릭
            await page.ClickAsync("#grp_loginBtn");
            Console.WriteLine("로그인 버튼 클릭");
            // 인증서로그인 버튼 클릭
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.ClickAsync("#btnCorpLogin");
            Console.WriteLine("인증서로그인 버튼 클릭");
            // 하드디스크 버튼 클릭
            await Task.Delay(1000);
            await page.WaitForSelectorAsync("#xwup_media_hdd");
            await page.ClickAsync("#xwup_media_hdd");
            Console.WriteLine("하드디스크 버튼 클릭");
            // 텍스트로 검색하여 공인인증서 클릭
            // string searchpublic = "가까운약국";
            var targetRowSelector_medi = $"tr:has-text('{title}')";
            await page.WaitForSelectorAsync(targetRowSelector_medi );
            var targetRow_public = await page.QuerySelectorAsync(targetRowSelector_medi );
            if (targetRow_public != null)
            {
                await page.WaitForTimeoutAsync(1000);
                await targetRow_public.ClickAsync();
                Console.WriteLine("공인인증서 선택 완료");
            }
            else
            {
                Console.WriteLine("공인인증서를 찾을 수 없습니다.");
            }
            // 텍스트로 검색하여 비밀번호 입력
            // string public_password = "8575!!";
            await page.FillAsync("#xwup_certselect_tek_input1", password);
            Console.WriteLine("비밀번호 입력");
            // 확인 버튼 클릭
            await page.ClickAsync("#xwup_OkButton");
            Console.WriteLine("확인 버튼 클릭");
            // 로그인을 실패하였을 때 창이 꺼지면서 비밀번호가 틀렸다는 메시지를 주는 코드 (예외처리)
            // 텍스트로 검색하여 약국 선택
            string searchmedi_id = "sunbi8575";
            var targetRowSelector_id = $"tr:has-text('{searchmedi_id}')";
            await page.WaitForSelectorAsync(targetRowSelector_id );
            var targetRow_medi = await page.QuerySelectorAsync(targetRowSelector_id );
            if (targetRow_medi != null)
            {
                var loginLink = await targetRow_medi.QuerySelectorAsync(".login");
                if (loginLink != null)
                {
                    await loginLink.ClickAsync();
                    Console.WriteLine("약국 선택 완료");
                }
                else
                {
                    Console.WriteLine("약국을 찾을 수 없습니다.");
                }
            }
            else
            {
                Console.WriteLine("약국을 찾을 수 없습니다.");
            }
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            // 그 전에 쓴 코드에는 오류가 떠서 3초 대기 후 종료되는 코드로 변경
            await Task.Delay(3000);
            await browser.CloseAsync();         
        }

        //Playwright 브라우저 설치 코드
        static void InstallPlaywrightBrowsers()
        {
            // 사용자 홈 디렉토리를 기반으로 playwright.ps1 스크립트 파일의 경로 설정
            string userHomeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string playwrightInstallScript = Path.Combine(userHomeDirectory, "Desktop", "Medi_Web2", "playwright.ps1");
            if (!File.Exists(playwrightInstallScript))
            {
                Console.WriteLine($"스크립트 파일이 존재하지 않습니다: {playwrightInstallScript}");
                return;
            }
            var processInfo = new ProcessStartInfo("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -File \"{playwrightInstallScript}\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using (var process = Process.Start(processInfo))
            {
                if (process != null)
                {
                    process.OutputDataReceived += (sender, args) => Console.WriteLine(args.Data);
                    process.ErrorDataReceived += (sender, args) => Console.WriteLine(args.Data);
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();
                }
            }
            Console.WriteLine("Playwright 브라우저가 성공적으로 설치되었습니다.");
        }
        //Playwright 브라우저가 깔려있는지 확인하는 코드(예외처리) 필요함

        //PowerShell 설치 코드
        static void InstallPowerShell()
        {
            string downloadUrl = "https://github.com/PowerShell/PowerShell/releases/download/v7.2.0/PowerShell-7.2.0-win-x64.msi";
            string outputFile = @"C:\Temp\PowerShell-7.2.0-win-x64.msi";
            using (WebClient webClient = new WebClient())
            {
                try
                {
                    webClient.DownloadFile(downloadUrl, outputFile);
                    Console.WriteLine($"PowerShell 다운로드가 완료되었습니다. 파일 경로: {outputFile}");

                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "msiexec",
                        Arguments = $"/i \"{outputFile}\" /quiet",
                        WindowStyle = ProcessWindowStyle.Hidden
                    };

                    using (var process = Process.Start(psi))
                    {
                        process?.WaitForExit();
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"오류가 발생했습니다: {ex.Message}");
                }
            }
        }

        //Powerehell이 깔려있는지 확인하는 코드
        static bool CheckIfPowerShellInstalled()
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-Command \"Get-Command pwsh -ErrorAction SilentlyContinue\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using (var process = Process.Start(processInfo))
            {
                if (process != null)
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    return !string.IsNullOrEmpty(output);
                }
            }
            return false;
        }
    }
}
