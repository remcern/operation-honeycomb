
using System;
using System.Collections;

public interface IRiddleEvaluator
{
    IEnumerator EvaluateRiddle(
        string riddleId,
        string riddle,
        string expectedAnswer,
        string playerAnswer,
        Action<OllamaService.RiddleResult, string> onComplete = null
    );
}
