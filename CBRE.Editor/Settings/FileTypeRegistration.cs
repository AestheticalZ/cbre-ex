﻿using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Win32;

namespace CBRE.Editor.Settings {
    public static class FileTypeRegistration {
        public const string ProgramId = "CBREEditor";
        public const string ProgramIdVer = "1";

        public static FileType[] GetSupportedExtensions() {
            return new[]
            {
                new FileType(".cbr", "Containment Breach Room", true, true),
                new FileType(".vmf", "Valve Map File", true, true),
                new FileType(".3dw", "Leadwerks 3D World Studio File", false, true),
                new FileType(".msl", "Mapscape 2 Level", false, true),
            };
        }

        private static string ExecutableLocation() {
            return Assembly.GetEntryAssembly().Location;
        }

        private static void AddExtensionHandler(string extension, string description) {
            using (var root = Registry.CurrentUser.OpenSubKey("Software\\Classes", true)) {
                if (root == null) return;

                using (var progId = root.CreateSubKey(ProgramId + extension + "." + ProgramIdVer)) {
                    if (progId == null) return;
                    progId.SetValue("", description);

                    using (var di = progId.CreateSubKey("DefaultIcon")) {
                        if (di != null) di.SetValue("", ExecutableLocation() + ",-40001");
                    }

                    using (var comm = progId.CreateSubKey("shell\\open\\command")) {
                        if (comm != null) comm.SetValue("", "\"" + ExecutableLocation() + "\" \"%1\"");
                    }

                    progId.SetValue("AppUserModelID", ProgramId);
                }
            }
        }

        private static void AssociateExtensionHandler(string extension) {
            using (var root = Registry.CurrentUser.OpenSubKey("Software\\Classes", true)) {
                if (root == null) return;

                using (var ext = root.CreateSubKey(extension)) {
                    if (ext == null) return;
                    ext.SetValue("", ProgramId + extension + "." + ProgramIdVer);
                    ext.SetValue("PerceivedType", "Document");

                    using (var openWith = ext.CreateSubKey("OpenWithProgIds")) {
                        if (openWith != null) openWith.SetValue(ProgramId + extension + "." + ProgramIdVer, string.Empty);
                    }
                }
            }
        }

        public static IEnumerable<string> GetRegisteredDefaultFileTypes() {
            using (var root = Registry.CurrentUser.OpenSubKey("Software\\Classes")) {
                if (root == null) yield break;

                foreach (var ft in GetSupportedExtensions()) {
                    using (var ext = root.OpenSubKey(ft.Extension)) {
                        if (ext == null) continue;
                        if (Convert.ToString(ext.GetValue("")) == ProgramId + ft.Extension + "." + ProgramIdVer) {
                            yield return ft.Extension;
                        }
                    }
                }
            }
        }

        public static void RegisterDefaultFileTypes(IEnumerable<string> extensions) {
#if DEBUG
            return;
#else
            foreach (var e in extensions) {
                var extension = e;
                if (!extension.StartsWith(".")) extension = "." + extension;
                AssociateExtensionHandler(extension);
            }
#endif
        }

        public static void RegisterFileTypes() {
#if DEBUG
            return;
#else
            try {
                foreach (var ft in GetSupportedExtensions()) {
                    AddExtensionHandler(ft.Extension, ft.Description);
                }
            } catch (UnauthorizedAccessException) {
                // security exception or some such
            }
#endif
        }
    }
}
