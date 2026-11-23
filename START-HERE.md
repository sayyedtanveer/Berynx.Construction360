# Import these Markdown files into Delta

1. Extract the ZIP **contents** directly into your existing local Git repository:

   `C:\Users\sayye\source\repos\Berynx.Construction360`

   The root must contain `AGENTS.md`, `README.md`, `START-HERE.md` and `docs/`, **not** an extra nested starter folder.
2. In PowerShell:

```powershell
cd C:\Users\sayye\source\repos\Berynx.Construction360
git status
git add AGENTS.md README.md START-HERE.md docs
git commit -m "Add Berynx BRD and AI development instructions"
git push origin main
```

3. Refresh Delta's project. In the first prompt, ask it to read `AGENTS.md`, `docs/requirements/BRD_v2.2.md` and `docs/implementation-plan.md` first.
4. Ask it to perform **Gate 0 project scaffolding only**, check real build results and stop for review.

The Word BRD may be kept alongside `.md` for reference; Markdown is the agent-friendly version. Source text, including historical v2.1 passages, is retained in the BRD. Later v2.2 completion sections take priority where they expressly supersede earlier statements.
