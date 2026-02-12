# VS Code PR Governance Extension Integration

## Overview
This integration enables real-time PR governance validation directly in VS Code, providing immediate feedback as developers write code and before they push changes.

## VS Code Extension Configuration

### 1. Task Configuration
```json
// .vscode/tasks.json
{
  "version": "2.0.0",
  "tasks": [
    {
      "label": "PR Governance Check",
      "type": "npm",
      "script": "pr-check",
      "problemMatcher": [],
      "presentation": {
        "reveal": "always",
        "panel": "new"
      },
      "group": {
        "kind": "test",
        "isDefault": true
      }
    },
    {
      "label": "PR Governance - Coverage Only",
      "type": "shell",
      "command": "npm run test:coverage && node .github/scripts/check-coverage.js",
      "problemMatcher": []
    },
    {
      "label": "PR Governance - Security Scan",
      "type": "shell",
      "command": "node .github/scripts/security-scan.js",
      "problemMatcher": []
    },
    {
      "label": "PR Governance - Breaking Changes",
      "type": "shell",
      "command": "node .github/scripts/detect-breaking-changes.js",
      "problemMatcher": []
    }
  ]
}
```

### 2. Settings Configuration
```json
// .vscode/settings.json
{
  "files.watcherExclude": {
    "**/node_modules": true,
    "**/coverage": true
  },
  
  // Auto-run PR check on save for specific files
  "runOnSave.commands": [
    {
      "match": "\\.(ts|js)$",
      "command": "workbench.action.tasks.runTask",
      "args": ["PR Governance - Quick Check"]
    }
  ],
  
  // GitHub Copilot custom instructions
  "github.copilot.advanced": {
    "inlineSuggestCount": 3,
    "authProvider": "github"
  },
  
  // Custom snippets for governance
  "github.copilot.enable": {
    "*": true
  },
  
  // Workspace-specific governance rules
  "prGovernance.enabled": true,
  "prGovernance.autoCheck": true,
  "prGovernance.checkOnSave": true,
  "prGovernance.minCoverage": 80,
  "prGovernance.blockOnFailure": true
}
```

### 3. Keybindings
```json
// .vscode/keybindings.json
[
  {
    "key": "ctrl+shift+p",
    "command": "workbench.action.tasks.runTask",
    "args": "PR Governance Check",
    "when": "editorTextFocus"
  },
  {
    "key": "ctrl+shift+t",
    "command": "workbench.action.tasks.runTask",
    "args": "PR Governance - Coverage Only"
  }
]
```

## GitHub Copilot Workspace Agent

### Agent Configuration
```markdown
# .github/copilot/pr-governance.md

You are a PR Governance Agent that helps developers identify issues before creating pull requests.

## When to Activate
Activate when the developer:
- Runs `@workspace /pr-check`
- Runs `@workspace /validate`
- Asks "is my PR ready?"
- Asks "what's missing in my changes?"
- Runs `@workspace /coverage`
- Runs `@workspace /security`

## Analysis Steps

### Step 1: Analyze Changed Files
```typescript
// Get list of changed files
const changedFiles = await getGitChangedFiles();

// Categorize files
const categories = {
  source: changedFiles.filter(f => f.startsWith('src/')),
  tests: changedFiles.filter(f => f.includes('.test.') || f.includes('.spec.')),
  docs: changedFiles.filter(f => f.endsWith('.md')),
  config: changedFiles.filter(f => f.includes('package.json') || f.includes('.env'))
};
```

### Step 2: Test Coverage Analysis
Check workspace files:
- Run coverage: `npm run test:coverage`
- Read `coverage/coverage-summary.json`
- Compare against policy: `.github/governance/coverage-policy.yml`

Report findings:
```
❌ BLOCKER: Test coverage is 65%, minimum required is 80%
   Files missing tests:
   - src/services/event.service.ts (0% coverage)
   - src/controllers/event.controller.ts (45% coverage)
   
   Action: Add unit tests for these files
```

### Step 3: Security Scan
Analyze code for:
1. Hardcoded secrets (search patterns: password=, api_key=, token=)
2. Vulnerable patterns (eval, innerHTML, dangerouslySetInnerHTML)
3. Dependency vulnerabilities (npm audit)
4. Blocked packages

Report findings:
```
❌ BLOCKER: Hardcoded API key found in src/config/api.ts:12
   Line: const API_KEY = "sk-1234567890abcdef";
   
   Action: Move to environment variable
   Fix: 
   - Add to .env: API_KEY=sk-1234567890abcdef
   - Update code: const API_KEY = process.env.API_KEY;
```

### Step 4: Breaking Changes Detection
Check for:
1. API endpoint modifications (controllers, routes)
2. Database schema changes (migrations)
3. Environment variable changes (.env.example)
4. Public interface changes (exported functions, types)
5. Major dependency updates

Report findings:
```
⚠️  WARNING: Potential breaking change detected
   - Modified API endpoint: DELETE /api/events/:id
   - Database migration added: 20240115_add_event_status.ts
   
   Required:
   - Update API documentation
   - Add migration guide in docs/migrations/
   - Update CHANGELOG.md
```

### Step 5: Documentation Check
Verify:
- README.md updated for new features
- API endpoints have OpenAPI/Swagger docs
- JSDoc comments for public functions
- CHANGELOG.md entry
- Migration guides for breaking changes

Report findings:
```
⚠️  WARNING: Documentation incomplete
   Missing:
   - ❌ README.md not updated
   - ❌ No JSDoc for EventService.createEvent()
   - ❌ No entry in CHANGELOG.md
   
   Suggestion: Add documentation before creating PR
```

### Step 6: Code Quality
Run:
- Linting: `npm run lint`
- Type checking: `npm run type-check`
- Format checking: `npm run format:check`

### Step 7: Dependency Validation
Check:
- package-lock.json updated when package.json changes
- No circular dependencies
- Outdated packages
- License compatibility

## Output Format

Provide a structured report:

```markdown
# PR Readiness Report

## 📊 Summary
- Files Changed: 15
- Test Coverage: 65% ❌ (Minimum: 80%)
- Security Issues: 2 ❌
- Breaking Changes: 1 ⚠️
- Documentation: Incomplete ⚠️

## 🚫 Blockers (Must Fix)
1. **Test Coverage Too Low**
   - Current: 65%
   - Required: 80%
   - Missing tests for:
     * src/services/event.service.ts
     * src/controllers/event.controller.ts
   
2. **Security: Hardcoded Secret**
   - File: src/config/api.ts:12
   - Issue: API key hardcoded
   - Fix: Use environment variable

## ❌ Errors (Should Fix)
1. **Missing JSDoc**
   - EventService.createEvent() missing documentation
   - Add JSDoc with @param and @returns

## ⚠️  Warnings (Recommended)
1. **Breaking Change Detected**
   - API endpoint modified
   - Add migration guide

2. **README Not Updated**
   - New feature added but README unchanged
   - Document new event registration flow

## ✅ Next Steps
1. Add unit tests to reach 80% coverage
2. Move API key to environment variable
3. Add JSDoc documentation
4. Update README and CHANGELOG
5. Run `npm run pr-check` to validate

## 🔧 Quick Fixes
Run these commands:
```bash
# Generate missing tests
@workspace Generate unit tests for EventService

# Add JSDoc
@workspace Add JSDoc comments to EventService methods

# Update documentation
@workspace Update README with event registration feature
```

After fixes, run: `npm run pr-check`
```

## Interactive Assistance

If developer asks for help:
- "How do I fix test coverage?" → Generate test code
- "What should the migration guide include?" → Provide template
- "How to move secrets to env?" → Show refactoring steps

## Success Criteria
PR is ready when:
- ✅ Test coverage ≥ 80%
- ✅ No security issues
- ✅ Breaking changes documented
- ✅ Code quality checks pass
- ✅ Documentation complete
```

## GitHub Copilot Commands

Add to `.github/copilot/commands.json`:
```json
{
  "commands": [
    {
      "name": "pr-check",
      "description": "Run complete PR governance validation",
      "prompt": "Analyze all changed files and check:\n1. Test coverage (minimum 80%)\n2. Security issues\n3. Breaking changes\n4. Documentation completeness\n5. Code quality\n\nProvide detailed report with blockers, errors, and warnings."
    },
    {
      "name": "coverage",
      "description": "Check test coverage for changed files",
      "prompt": "Check test coverage for all changed files. Identify files with <80% coverage and suggest tests to add."
    },
    {
      "name": "security",
      "description": "Scan for security issues",
      "prompt": "Scan changed files for:\n- Hardcoded secrets\n- Vulnerable patterns (eval, innerHTML)\n- Dependency vulnerabilities\n- Blocked packages\n\nReport all findings with severity and fix suggestions."
    },
    {
      "name": "breaking",
      "description": "Detect breaking changes",
      "prompt": "Analyze changes for breaking changes:\n- API modifications\n- Database schema changes\n- Environment variable changes\n- Public interface changes\n\nProvide migration guidance if needed."
    },
    {
      "name": "docs",
      "description": "Check documentation completeness",
      "prompt": "Verify documentation:\n- README updated\n- API docs (OpenAPI/Swagger)\n- JSDoc for public functions\n- CHANGELOG entry\n\nList what's missing."
    },
    {
      "name": "fix-coverage",
      "description": "Generate tests for low coverage files",
      "prompt": "For each file with <80% coverage, generate comprehensive unit tests including:\n- Happy path\n- Error cases\n- Edge cases\n- Mock dependencies"
    }
  ]
}
```

## Live Validation in VS Code

### Real-time Feedback Extension
Create custom VS Code extension for live validation:

```typescript
// extension.ts
import * as vscode from 'vscode';
import { PRGovernanceValidator } from './validators/pr-validator';

export function activate(context: vscode.ExtensionContext) {
  // Status bar item
  const statusBarItem = vscode.window.createStatusBarItem(
    vscode.StatusBarAlignment.Left,
    100
  );
  statusBarItem.text = "$(check) PR Ready";
  statusBarItem.command = 'prGovernance.check';
  statusBarItem.show();
  
  // Register commands
  const checkCommand = vscode.commands.registerCommand(
    'prGovernance.check',
    async () => {
      const validator = new PRGovernanceValidator();
      const result = await validator.validate();
      
      // Update status bar
      if (result.passed) {
        statusBarItem.text = "$(check) PR Ready";
        statusBarItem.backgroundColor = undefined;
      } else {
        statusBarItem.text = `$(x) ${result.blockers.length} Blockers`;
        statusBarItem.backgroundColor = new vscode.ThemeColor(
          'statusBarItem.errorBackground'
        );
      }
      
      // Show report in webview
      showReportWebview(result);
    }
  );
  
  // Auto-check on save
  const saveWatcher = vscode.workspace.onDidSaveTextDocument(
    async (document) => {
      const config = vscode.workspace.getConfiguration('prGovernance');
      if (config.get('checkOnSave')) {
        vscode.commands.executeCommand('prGovernance.check');
      }
    }
  );
  
  context.subscriptions.push(checkCommand, saveWatcher, statusBarItem);
}

function showReportWebview(result: any) {
  const panel = vscode.window.createWebviewPanel(
    'prGovernanceReport',
    'PR Governance Report',
    vscode.ViewColumn.Two,
    {}
  );
  
  panel.webview.html = generateReportHTML(result);
}

function generateReportHTML(result: any): string {
  return `
    <!DOCTYPE html>
    <html>
    <head>
      <style>
        body { font-family: system-ui; padding: 20px; }
        .blocker { color: #f44336; }
        .error { color: #ff9800; }
        .warning { color: #ffc107; }
        .success { color: #4caf50; }
        .section { margin: 20px 0; }
      </style>
    </head>
    <body>
      <h1>PR Governance Report</h1>
      
      <div class="section">
        <h2>Summary</h2>
        <p>Test Coverage: ${result.summary.test_coverage}%</p>
        <p>Security Issues: ${result.summary.security_issues}</p>
        <p>Breaking Changes: ${result.summary.breaking_changes}</p>
      </div>
      
      ${result.blockers.length > 0 ? `
        <div class="section blocker">
          <h2>🚫 Blockers</h2>
          <ul>
            ${result.blockers.map(b => `
              <li>
                <strong>${b.category}:</strong> ${b.message}
                <br><em>Action: ${b.required_action}</em>
              </li>
            `).join('')}
          </ul>
        </div>
      ` : ''}
      
      <div class="section ${result.passed ? 'success' : 'blocker'}">
        <h2>${result.passed ? '✅ PR Ready' : '❌ Fix Issues Before PR'}</h2>
      </div>
    </body>
    </html>
  `;
}
```

## Usage Examples

### 1. Before Pushing Code
```bash
# In VS Code terminal
npm run pr-check

# Or use Copilot
@workspace /pr-check
```

### 2. Check Specific Aspects
```bash
# Just coverage
npm run pr-check:coverage

# Just security
npm run pr-check:security

# Just breaking changes
npm run pr-check:breaking
```

### 3. Fix Issues with Copilot
```
Developer: @workspace /pr-check

Copilot: ❌ PR Not Ready

Blockers:
1. Test coverage 65%, need 80%
   Missing: src/services/event.service.ts

Developer: @workspace /fix-coverage for EventService

Copilot: [Generates comprehensive test suite]

Developer: npm run pr-check

Copilot: ✅ All checks passed! Ready to create PR
```

## Integration with GitHub Actions

```yaml
# .github/workflows/pr-governance.yml
name: PR Governance Check

on:
  pull_request:
    types: [opened, synchronize]

jobs:
  governance:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup Node
        uses: actions/setup-node@v3
        with:
          node-version: 18
      
      - name: Install dependencies
        run: npm ci
      
      - name: Run PR Governance Check
        id: governance
        run: |
          npm run pr-check > pr-report.txt
          echo "report<<EOF" >> $GITHUB_OUTPUT
          cat pr-report.txt >> $GITHUB_OUTPUT
          echo "EOF" >> $GITHUB_OUTPUT
      
      - name: Comment PR
        uses: actions/github-script@v6
        with:
          script: |
            github.rest.issues.createComment({
              issue_number: context.issue.number,
              owner: context.repo.owner,
              repo: context.repo.repo,
              body: `## PR Governance Report\n\n\`\`\`\n${{ steps.governance.outputs.report }}\n\`\`\``
            })
      
      - name: Block merge if failed
        if: failure()
        run: |
          echo "::error::PR governance checks failed. Fix issues before merging."
          exit 1
```