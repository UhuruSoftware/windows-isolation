// -----------------------------------------------------------------------
// <copyright file="UserImpersonator.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// Code from http://www.codeproject.com/Articles/10090/A-small-C-Class-for-impersonating-a-User
// -----------------------------------------------------------------------

namespace Uhuru.Prison.Utilities
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Security.Principal;
    using Microsoft.Win32.SafeHandles;

    /// <summary>
    /// Impersonation of a user. Allows to execute code under another
    /// user context.
    /// Please note that the account that instantiates the Impersonator class
    /// needs to have the 'Act as part of operating system' privilege set.
    /// </summary>
    /// <remarks>
    /// This class is based on the information in the Microsoft knowledge base
    /// article http://support.microsoft.com/default.aspx?scid=kb;en-us;Q306158
    /// Encapsulate an instance into a using-directive like e.g.:
    /// </remarks>
    public class UserImpersonator : IDisposable
    {
        /// <summary>
        /// Interactive logon.
        /// </summary>
        private const int Logon32LogonInteractive = 2;

        /// <summary>
        /// Default logon provider.
        /// </summary>
        private const int Logon32ProviderDefault = 0;

        /// <summary>
        /// Disposed flag.
        /// </summary>
        private bool disposed = false;

        /// <summary>
        /// Impersonation context.
        /// </summary>
        private WindowsImpersonationContext impersonationContext = null;

        /// <summary>
        /// User profile handle.
        /// </summary>
        private IntPtr profileHandle;

        /// <summary>
        /// User Duplicate Token handle.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Justification = "Keep it simple.")]
        private IntPtr userToken;
        private SafeHandle SafeRegistryHandle;

        /// <summary>
        /// Initializes a new instance of the UserImpersonator class.
        /// Starts the impersonation with the given credentials.
        /// Please note that the account that instantiates the Impersonator class
        /// needs to have the 'Act as part of operating system' privilege set.
        /// </summary>
        /// <param name="userName">The name of the user to act as.</param>
        /// <param name="domainName">The domain name of the user to act as.</param>
        /// <param name="password">The password of the user to act as.</param>
        /// <param name="loadUserProfile">if set to <c>true</c> [load user profile].</param>
        public UserImpersonator(string userName, string domainName, string password, bool loadUserProfile)
        {
            this.ImpersonateValidUser(userName, domainName, password, loadUserProfile);
        }

        /// <summary>
        /// Finalizes an instance of the UserImpersonator class.
        /// </summary>
        ~UserImpersonator()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Deletes the user profile.
        /// </summary>
        /// <param name="userName">Name of the user.</param>
        /// <param name="domainName">The domain name of the user to act as.</param>
        public static void DeleteUserProfile(string userName, string domainName)
        {
            NTAccount ntaccount = new NTAccount(domainName, userName);
            string userSid = ntaccount.Translate(typeof(SecurityIdentifier)).Value;

            bool retry = true;
            int retries = 2;
            while (retry && retries > 0)
            {
                retry = false;

                if (!DeleteProfile(userSid, null, null))
                {
                    int errorCode = Marshal.GetLastWin32Error();

                    // Error Code 2: The user profile was not created or was already deleted
                    if (errorCode == 2)
                    {
                        return;
                    }
                    // Error Code 87: The user profile is still loaded.
                    else if (errorCode == 87)
                    {
                        retry = true;
                        retries--;
                    }
                    else
                    {
                        throw new Win32Exception(errorCode);
                    }
                }
            }
        }

        /// <summary>
        /// Disposes the current object.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Gets the registry handle.
        /// </summary>
        /// <returns>The impersonated registry handle.</returns>
        public SafeRegistryHandle GetRegistryHandle()
        {
            return new SafeRegistryHandle(profileHandle, false);
        }

        /// <summary>
        /// Disposes the current object.
        /// </summary>
        /// <param name="disposing">True if disposing from user code.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                // Suppress finalization of this disposed instance. 
                if (disposing)
                {
                    if (this.impersonationContext != null)
                    {
                        this.impersonationContext.Undo();
                        this.impersonationContext.Dispose();
                        this.impersonationContext = null;
                    }
                }

                if (this.profileHandle != IntPtr.Zero)
                {
                    UnloadUserProfile(this.userToken, this.profileHandle);
                    this.profileHandle = IntPtr.Zero;
                }

                if (this.userToken != IntPtr.Zero)
                {
                    CloseHandle(this.userToken);
                    this.userToken = IntPtr.Zero;
                }

                this.disposed = true;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass", Justification = "Keeping everything in one file for clarity.")]
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int LogonUser(string userName, string domain, string password, int logonType, int logonProvider, ref IntPtr token);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass", Justification = "Keeping everything in one file for clarity.")]
        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int DuplicateToken(IntPtr token, int impersonationLevel, ref IntPtr newToken);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass", Justification = "Keeping everything in one file for clarity.")]
        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RevertToSelf();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass", Justification = "Keeping everything in one file for clarity.")]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr handle);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass", Justification = "Keeping everything in one file for clarity.")]
        [DllImport("userenv.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAsAttribute(UnmanagedType.Bool)]
        private static extern bool LoadUserProfile([In] System.IntPtr token, ref ProfileInfo profileInfo);

        // http://support.microsoft.com/kb/196070
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass", Justification = "Keeping everything in one file for clarity.")]
        [DllImport("userenv.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAsAttribute(UnmanagedType.Bool)]
        private static extern bool UnloadUserProfile([In] System.IntPtr token, [In] System.IntPtr profile);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass", Justification = "Keeping everything in one file for clarity.")]
        [DllImport("userenv.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAsAttribute(UnmanagedType.Bool)]
        private static extern bool DeleteProfile([In] string sidString, [In, Optional] string profilePath, [In, Optional] string computerName);

        /// <summary>
        /// Does the actual impersonation.
        /// </summary>
        /// <param name="userName">The name of the user to act as.</param>
        /// <param name="domain">The domain name of the user to act as.</param>
        /// <param name="password">The password of the user to act as.</param>
        /// <param name="loadUserProfile">if set to <c>true</c> [load user profile].</param>
        private void ImpersonateValidUser(string userName, string domain, string password, bool loadUserProfile)
        {
            this.profileHandle = IntPtr.Zero;
            this.userToken = IntPtr.Zero;

            WindowsIdentity tempWindowsIdentity = null;
            IntPtr token = IntPtr.Zero;

            ProfileInfo profileInfo = new ProfileInfo();

            profileInfo.Size = Marshal.SizeOf(profileInfo.GetType());
            profileInfo.Flags = 0x1;
            profileInfo.UserName = userName;

            profileInfo.ProfilePath = null;
            profileInfo.DefaultPath = null;

            profileInfo.PolicyPath = null;
            profileInfo.ServerName = domain;

            try
            {
                if (!RevertToSelf())
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                if (LogonUser(userName, domain, password, Logon32LogonInteractive, Logon32ProviderDefault, ref token) == 0)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                if (DuplicateToken(token, 2, ref this.userToken) == 0)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                if (loadUserProfile && !LoadUserProfile(this.userToken, ref profileInfo))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                // Save the handle for dispose
                this.profileHandle = profileInfo.Profile;

                using (tempWindowsIdentity = new WindowsIdentity(this.userToken))
                {
                    this.impersonationContext = tempWindowsIdentity.Impersonate();
                }
            }
            finally
            {
                if (token != IntPtr.Zero)
                {
                    CloseHandle(token);
                }
            }
        }

        /// <summary>
        /// Profile Info structure.
        /// </summary>
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct ProfileInfo
        {
            /// <summary>
            /// Structure filed.
            /// </summary>
            public int Size;

            /// <summary>
            /// Structure filed.
            /// </summary>
            public int Flags;

            /// <summary>
            /// Structure filed.
            /// </summary>
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.LPTStr)]
            public string UserName;

            /// <summary>
            /// Structure filed.
            /// </summary>
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.LPTStr)]
            public string ProfilePath;

            /// <summary>
            /// Structure filed.
            /// </summary>
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.LPTStr)]
            public string DefaultPath;

            /// <summary>
            /// Structure filed.
            /// </summary>
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.LPTStr)]
            public string ServerName;

            /// <summary>
            /// Policy path filed.
            /// </summary>
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.LPTStr)]
            public string PolicyPath;

            /// <summary>
            /// Profile field.
            /// </summary>
            public System.IntPtr Profile;
        }
    }
}