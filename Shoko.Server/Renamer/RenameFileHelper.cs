using System;

using NLog;
using Shoko.Models.Server;
using Shoko.Server.Models;
using Shoko.Server.Renamer;
using Shoko.Server.Repositories;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Nancy.Extensions;

namespace Shoko.Server
{
    public class RenameFileHelper
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private static IDictionary<string, Type> ScriptImplementations = new Dictionary<string, Type>();
        public static IDictionary<string, string> ScriptDescriptions { get; } = new Dictionary<string, string>();

        public static IRenamer GetRenamer()
        {
            var script = RepoFactory.RenameScript.GetDefaultScript();
            if (script == null) return null;
            return GetRenamerFor(script);
        }

        public static IRenamer GetRenamerWithFallback()
        {
            var script = RepoFactory.RenameScript.GetDefaultOrFirst();
            if (script == null) return null;

            return GetRenamerFor(script);
        }

        public static IRenamer GetRenamer(string scriptName)
        {
            var script = RepoFactory.RenameScript.GetByName(scriptName);
            if (script == null) return null;

            return GetRenamerFor(script);
        }

        private static IRenamer GetRenamerFor(RenameScript script)
        {
            if (!ScriptImplementations.ContainsKey(script.RenamerType))
                return null;

            try
            {
                return (IRenamer) Activator.CreateInstance(ScriptImplementations[script.RenamerType], script);
            }
            catch (MissingMethodException)
            {
                return (IRenamer)Activator.CreateInstance(ScriptImplementations[script.RenamerType]);
            }
        }

        public static void InitialiseRenamers()
        {
            List<Assembly> asse = new List<Assembly>();
            Assembly assembly = Assembly.GetExecutingAssembly();
            UriBuilder uri = new UriBuilder(assembly.GetName().CodeBase);
            string dirname = Path.GetDirectoryName(Uri.UnescapeDataString(uri.Path));
            logger.Info("InitialiseRenamers: dir: {0}", dirname);
            asse.Add(Assembly.GetCallingAssembly()); //add this to dynamically load as well.
            string[] files = Directory.GetFiles(dirname, $"Renamer.*.dll", SearchOption.AllDirectories);
            logger.Info("InitialiseRenamers: found {0}",files.Length);
            foreach (string dll in files)
            {
                try
                {
                    logger.Info("InitialiseRenamers: Loading: {0}",dll);
                    asse.Add(Assembly.LoadFile(dll));
                    logger.Info("InitialiseRenamers: {0} loaded",dll);
                }
                catch (FileLoadException ex)
                {
                    logger.Error("InitialiseRenamers: FileLoadException: {0}",ex.ToString(),ex);
                }
                catch (BadImageFormatException ex)
                {
                    logger.Error("InitialiseRenamers: BadImageFormarException: {0}",ex.ToString(),ex);
                }
            }

            var implementations = asse.SelectMany(a => a.GetTypes())
                .Where(a => a.GetInterfaces().Contains(typeof(IRenamer)));
            logger.Info("InitialiseRenamers: IRenamer implementations: {0}",implementations.Count());
            foreach (var implementation in implementations)
            {
                logger.Info("InitialiseRenamers: impl: {0}",implementation.FullName);
                IEnumerable<RenamerAttribute> attributes = implementation.GetCustomAttributes<RenamerAttribute>();
                logger.Info("InitialiseRenamers: attributes: {0}",attributes.Count());
                foreach ((string key, string desc) in attributes.Select(a => (key: a.RenamerId, desc: a.Description)))
                {
                    logger.Info("InitialiseRenamers: RenamerId: {0} Description: {1}",key, desc);
                    if (key == null) continue;
                    if (ScriptImplementations.ContainsKey(key))
                    {
                        logger.Warn($"[RENAMER] Warning Duplicate renamer key \"{key}\" of types {implementation}@{implementation.GetAssemblyPath()} and {ScriptImplementations[key]}@{ScriptImplementations[key].GetAssemblyPath()}");
                        continue;
                    }
                    ScriptImplementations.Add(key, implementation);
                    ScriptDescriptions.Add(key, desc);
                }
            }
        }
        public static string GetNewFileName(SVR_VideoLocal_Place vid)
        {
            try
            {
                return GetRenamer()?.GetFileName(vid);
            }
            catch (Exception ex)
            {
                logger.Error(ex, ex.ToString());
                return string.Empty;
            }
        }
    }
}