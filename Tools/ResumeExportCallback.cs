var pending=UnityEditor.EditorApplication.delayCall?.GetInvocationList().FirstOrDefault(d=>d.Method.DeclaringType.FullName=="PipelineEvaluation.PipelineEval_c35f2d9d+<>c__DisplayClass0_0" && d.Method.Name=="<Execute>b__5");
if(pending==null)return "No matching pending export";
UnityEditor.EditorApplication.delayCall-=(UnityEditor.EditorApplication.CallbackFunction)pending;
pending.DynamicInvoke();
return "Export callback completed";
