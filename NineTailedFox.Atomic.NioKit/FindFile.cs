using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NineTailedFox.Atomic.NioKit
{
    public sealed class FindFile
    {
        private readonly string[] _rootPaths;

        /// <summary>
        /// 单路径构造函数
        /// </summary>
        /// <param name="path">根目录路径</param>
        /// <exception cref="ArgumentNullException">路径为空</exception>
        /// <exception cref="DirectoryNotFoundException">路径不存在</exception>
        public FindFile(string path)
        {
            _rootPaths = ValidateAndStorePaths([path]);
        }

        /// <summary>
        /// 多路径构造函数
        /// </summary>
        /// <param name="paths">根目录路径集合</param>
        /// <exception cref="ArgumentNullException">路径集合为空</exception>
        /// <exception cref="DirectoryNotFoundException">所有路径均无效</exception>
        public FindFile(IEnumerable<string> paths)
        {
            if (paths is null || !paths.Any())
                throw new ArgumentNullException(nameof(paths), "Path collection cannot be empty");
            
            _rootPaths = ValidateAndStorePaths(paths);
        }

        private string[] ValidateAndStorePaths(IEnumerable<string> paths)
        {
            var validPaths = new List<string>();
            var errors = new List<Exception>();

            foreach (var path in paths)
            {
                try
                {
                    string fullPath = System.IO.Path.GetFullPath(path);
                    if (Directory.Exists(fullPath))
                        validPaths.Add(fullPath);
                    else
                        errors.Add(new DirectoryNotFoundException($"Directory not found: {path}"));
                }
                catch (Exception ex) when (ex is ArgumentException || 
                                          ex is PathTooLongException || 
                                          ex is NotSupportedException)
                {
                    errors.Add(ex);
                }
            }

            if (validPaths.Count == 0)
            {
                throw new AggregateException(
                    "All specified paths are invalid", 
                    errors
                );
            }

            return validPaths.ToArray();
        }

        /// <summary>
        /// 精确文件名匹配（绝对模式）
        /// 检查文件名是否*包含*指定字符串（不区分大小写）
        /// </summary>
        /// <param name="name">要匹配的文件名子串</param>
        /// <returns>匹配的文件完整路径</returns>
        public IEnumerable<string> Get(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Search name cannot be empty", nameof(name));

            return EnumerateFiles(name, isWildcard: false);
        }

        /// <summary>
        /// 通配符搜索（支持 * 和 ?）
        /// </summary>
        /// <param name="pattern">通配符模式（如 *.log, error_?.txt）</param>
        /// <returns>匹配的文件完整路径</returns>
        public IEnumerable<string> GetBlurry(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                throw new ArgumentException("Search pattern cannot be empty", nameof(pattern));

            return EnumerateFiles(pattern, isWildcard: true);
        }

        /// <summary>
        /// 核心枚举逻辑（分离异常处理与迭代器）
        /// </summary>
        private IEnumerable<string> EnumerateFiles(string searchTerm, bool isWildcard)
        {
            foreach (var root in _rootPaths)
            {
				// 通配符模式：委托给系统API处理
				if (isWildcard)
				{
					foreach (var file in SearchWithWildcards(root, searchTerm))
						yield return file;
				}
				// 绝对模式：精确子串匹配
				else
				{
					foreach (var file in SearchWithExactMatch(root, searchTerm))
						yield return file;
				}
            }
        }

        /// <summary>
        /// 通配符搜索实现（安全分离 yield return）
        /// </summary>
        private IEnumerable<string> SearchWithWildcards(string root, string pattern)
        {
            try
            {
                Directory.EnumerateFiles(
                    root,
                    pattern,
                    new EnumerationOptions {
                        RecurseSubdirectories = true,
                        IgnoreInaccessible = true,
                        MatchCasing = MatchCasing.CaseInsensitive,
                        MaxRecursionDepth = 32
                    }
                );
            }
            catch (Exception ex) when (
                ex is ArgumentException || 
                ex is PathTooLongException)
            {
                yield break; // 无效通配符模式直接跳过
            }
        }

        /// <summary>
        /// 精确匹配实现（安全分离 yield return）
        /// </summary>
        private IEnumerable<string> SearchWithExactMatch(string root, string substring)
        {
			var files = Directory.EnumerateFiles(
				root,
				"*", // 先获取所有文件
				new EnumerationOptions {
					RecurseSubdirectories = true,
					IgnoreInaccessible = true,
					MatchCasing = MatchCasing.CaseInsensitive,
					MaxRecursionDepth = 32
				}
			);

			foreach (var file in files)
			{
				if (System.IO.Path.GetFileName(file).Contains(
						substring, 
						StringComparison.OrdinalIgnoreCase))
				{
					yield return file; // ✅ 安全位置
				}
			}
        }
    }
}