using System.Collections.Generic;
using System.Text;

namespace ShrinkCart.Core
{
    /// <summary>
    /// 记录各功能因 patch 安装失败、游戏成员缺失或运行期异常而不可用的原因。
    /// 功能可用 = 自身没有问题且所有依赖（<see cref="FeatureIds.DependenciesOf"/>）都没有问题。
    /// </summary>
    internal sealed class FeatureAvailability
    {
        private readonly List<string>[] _problems = new List<string>[FeatureIds.Count];

        internal void MarkUnavailable(FeatureId feature, string reason)
        {
            List<string> problems = _problems[(int)feature];
            if (problems == null)
            {
                problems = new List<string>();
                _problems[(int)feature] = problems;
            }

            if (!problems.Contains(reason))
            {
                problems.Add(reason);
            }
        }

        internal bool IsAvailable(FeatureId feature)
        {
            if (HasProblems(feature))
            {
                return false;
            }

            FeatureId[] dependencies = FeatureIds.DependenciesOf(feature);
            for (int i = 0; i < dependencies.Length; i++)
            {
                if (HasProblems(dependencies[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>启动摘要：列出不可用的功能及原因；全部可用时返回 "all features available"。</summary>
        internal string Describe()
        {
            StringBuilder builder = null;
            for (int i = 0; i < FeatureIds.Count; i++)
            {
                FeatureId feature = (FeatureId)i;
                if (IsAvailable(feature))
                {
                    continue;
                }

                builder = builder == null ? new StringBuilder("Unavailable features: ") : builder.Append("; ");
                builder.Append(feature);
                List<string> problems = _problems[i];
                builder.Append(problems != null && problems.Count > 0
                    ? " (" + string.Join(", ", problems.ToArray()) + ")"
                    : " (dependency unavailable)");
            }

            return builder == null ? "All features available." : builder.ToString();
        }

        private bool HasProblems(FeatureId feature)
        {
            List<string> problems = _problems[(int)feature];
            return problems != null && problems.Count > 0;
        }
    }
}
