using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DecisionEvaluator
{
    public class GroupScore
    {
        public ResourceType resource;
        public int totalDelta; // acumulado
        public int finalValue; // 0..100
        public float combinedScore; // metric used for ranking
    }

    public class EvaluationResult
    {
        public List<GroupScore> rankings = new List<GroupScore>();
        public string summaryText;
    }

    // Parámetros ajustables
    private const float outcomeWeight = 1.0f; // cuánto pesa el valor final (vs. acumulado)
    private static readonly float[] thresholds = new float[] { 30f, 10f, -10f, -30f }; // para etiquetas

    public static EvaluationResult Evaluate(Dictionary<ResourceType,int> totals, Dictionary<ResourceType,int> finalValues)
    {
        var result = new EvaluationResult();

        var all = Enum.GetValues(typeof(ResourceType)).Cast<ResourceType>();

        foreach (var r in all)
        {
            totals.TryGetValue(r, out int tot);
            finalValues.TryGetValue(r, out int fin);
            float combined = tot + (fin - 50) * outcomeWeight; // fin-50 centra en 0
            result.rankings.Add(new GroupScore {
                resource = r,
                totalDelta = tot,
                finalValue = fin,
                combinedScore = combined
            });
        }

        // ordenar por combinedScore descendente
        result.rankings = result.rankings.OrderByDescending(g => g.combinedScore).ToList();

        // construir resumen
        var top = result.rankings.First();
        var bottom = result.rankings.Last();

        string topName = GroupName(top.resource);
        string bottomName = GroupName(bottom.resource);

        string topLabel = LabelFor(top.combinedScore);
        string bottomLabel = LabelFor(bottom.combinedScore);

        result.summaryText =
            $"Evaluación final:\n" +
            $"- Más favorecido: {topName} ({topLabel}). Puntos: {Mathf.RoundToInt(top.combinedScore)}. Valor final: {top.finalValue}.\n" +
            $"- Más perjudicado: {bottomName} ({bottomLabel}). Puntos: {Mathf.RoundToInt(bottom.combinedScore)}. Valor final: {bottom.finalValue}.\n\n" +
            "Desglose por grupo:\n";

        foreach (var g in result.rankings)
        {
            result.summaryText += $"- {GroupName(g.resource)}: delta acumulado {g.totalDelta}, valor final {g.finalValue} → {LabelFor(g.combinedScore)}.\n";
        }

        return result;
    }

    private static string GroupName(ResourceType r)
    {
        return r switch
        {
            ResourceType.Prisma => "Comunidad LGBT+",
            ResourceType.RaicesAntiguas => "Pueblos indígenas",
            ResourceType.AlmaDeHierro => "Personas con discapacidad",
            ResourceType.FeEterna => "Grupos religiosos",
            ResourceType.Oro => "Dinero / Capital",
            _ => r.ToString()
        };
    }

    private static string LabelFor(float score)
    {
        if (score >= thresholds[0]) return "muy favorecido";
        if (score >= thresholds[1]) return "favorecido";
        if (score > thresholds[2]) return "neutral";
        if (score > thresholds[3]) return "perjudicado";
        return "muy perjudicado";
    }
}
