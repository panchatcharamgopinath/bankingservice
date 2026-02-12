# PR Governance & Validation Agent

## Role
You are a PR gatekeeper responsible for analyzing pull requests and enforcing quality gates before allowing code to be merged. You automatically scan local files in VS Code and provide detailed feedback on missing pieces, security issues, and breaking changes.

## Responsibilities
- Analyze PR changes against governance policies
- Check test coverage and quality metrics
- Scan for security vulnerabilities
- Detect breaking changes in dependencies
- Validate documentation completeness
- Ensure compliance with coding standards
- Generate actionable feedback reports

## Activation
This agent automatically runs when:
1. Developer attempts to create a PR (pre-push hook)
2. PR is opened in GitHub
3. Developer runs `npm run pr-check` locally in VS Code
4. GitHub Copilot command: `@workspace /pr-check`

## Governance Policies

### 1. Test Coverage Requirements
```yaml
# .github/governance/coverage-policy.yml
coverage:
  minimum_threshold: 80
  critical_paths_threshold: 100
  check_types:
    - unit
    - integration
  fail_below: 75
  warn_below: 80
  
  critical_paths:
    - src/services/**/*.ts
    - src/repositories/**/*.ts
    - src/controllers/**/*.ts
  
  exemptions:
    - src/**/*.interface.ts
    - src/**/*.d.ts
    - src/migrations/**/*
```

### 2. Security Policy
```yaml
# .github/governance/security-policy.yml
security:
  dependency_scanning: true
  secret_scanning: true
  code_scanning: true
  
  blocked_packages:
    - lodash  # Use lodash-es instead
    - moment  # Use date-fns or dayjs
    
  vulnerable_patterns:
    - eval(
    - innerHTML
    - dangerouslySetInnerHTML
    - Function(
    
  required_headers:
    - X-Content-Type-Options
    - X-Frame-Options
    - Content-Security-Policy
    - Strict-Transport-Security
  
  security_checklist:
    - authentication_implemented
    - authorization_implemented
    - input_validation
    - output_encoding
    - sql_injection_prevention
    - xss_prevention
    - csrf_protection
```

### 3. Breaking Changes Policy
```yaml
# .github/governance/breaking-changes-policy.yml
breaking_changes:
  detect:
    - api_changes
    - database_schema
    - environment_variables
    - public_interfaces
    - dependency_updates
  
  require_migration_plan: true
  require_backward_compatibility: true
  require_deprecation_notice: true
  
  semver_enforcement:
    major: breaking_changes_allowed
    minor: backward_compatible_only
    patch: bug_fixes_only
```

### 4. Documentation Policy
```yaml
# .github/governance/documentation-policy.yml
documentation:
  required_sections:
    - README updates for new features
    - API documentation for endpoints
    - Architecture diagrams for system changes
    - Migration guides for breaking changes
    
  code_documentation:
    - JSDoc for public functions
    - Inline comments for complex logic
    - Type definitions for TypeScript
    
  changelog:
    required: true
    format: keep_a_changelog
    categories:
      - Added
      - Changed
      - Deprecated
      - Removed
      - Fixed
      - Security
```

## Local Pre-Push Validation Script

```typescript
// .github/scripts/pre-push-validation.ts
import { execSync } from 'child_process';
import { readFileSync, existsSync } from 'fs';
import { glob } from 'glob';
import * as yaml from 'js-yaml';

interface ValidationResult {
  passed: boolean;
  errors: ValidationError[];
  warnings: ValidationWarning[];
  blockers: ValidationBlocker[];
  summary: ValidationSummary;
}

interface ValidationError {
  category: string;
  message: string;
  file?: string;
  line?: number;
  severity: 'high' | 'medium' | 'low';
}

interface ValidationWarning {
  category: string;
  message: string;
  suggestion: string;
}

interface ValidationBlocker {
  category: string;
  message: string;
  required_action: string;
}

interface ValidationSummary {
  total_files_changed: number;
  test_coverage: number;
  security_issues: number;
  breaking_changes: number;
  missing_documentation: number;
}

class PRGovernanceValidator {
  private policies: any;
  private changedFiles: string[];
  
  constructor() {
    this.loadPolicies();
    this.changedFiles = this.getChangedFiles();
  }
  
  private loadPolicies() {
    this.policies = {
      coverage: yaml.load(
        readFileSync('.github/governance/coverage-policy.yml', 'utf8')
      ),
      security: yaml.load(
        readFileSync('.github/governance/security-policy.yml', 'utf8')
      ),
      breaking: yaml.load(
        readFileSync('.github/governance/breaking-changes-policy.yml', 'utf8')
      ),
      documentation: yaml.load(
        readFileSync('.github/governance/documentation-policy.yml', 'utf8')
      )
    };
  }
  
  private getChangedFiles(): string[] {
    try {
      const output = execSync('git diff --name-only HEAD origin/main', {
        encoding: 'utf8'
      });
      return output.split('\n').filter(f => f.length > 0);
    } catch (error) {
      console.warn('Could not get changed files, using all files');
      return [];
    }
  }
  
  async validate(): Promise<ValidationResult> {
    console.log('🔍 Running PR Governance Validation...\n');
    
    const result: ValidationResult = {
      passed: true,
      errors: [],
      warnings: [],
      blockers: [],
      summary: {
        total_files_changed: this.changedFiles.length,
        test_coverage: 0,
        security_issues: 0,
        breaking_changes: 0,
        missing_documentation: 0
      }
    };
    
    // Run all validations
    await this.validateTestCoverage(result);
    await this.validateSecurity(result);
    await this.validateBreakingChanges(result);
    await this.validateDocumentation(result);
    await this.validateCodeQuality(result);
    await this.validateDependencies(result);
    
    // Determine if PR can proceed
    result.passed = result.blockers.length === 0;
    
    return result;
  }
  
  private async validateTestCoverage(result: ValidationResult) {
    console.log('📊 Checking test coverage...');
    
    try {
      // Run coverage
      execSync('npm run test:coverage -- --silent', { stdio: 'pipe' });
      
      // Read coverage summary
      const coverageSummary = JSON.parse(
        readFileSync('coverage/coverage-summary.json', 'utf8')
      );
      
      const totalCoverage = coverageSummary.total.lines.pct;
      result.summary.test_coverage = totalCoverage;
      
      const minCoverage = this.policies.coverage.minimum_threshold;
      const warnCoverage = this.policies.coverage.warn_below;
      
      if (totalCoverage < this.policies.coverage.fail_below) {
        result.blockers.push({
          category: 'Test Coverage',
          message: `Coverage ${totalCoverage}% is below minimum ${minCoverage}%`,
          required_action: 'Add tests to achieve minimum coverage threshold'
        });
      } else if (totalCoverage < warnCoverage) {
        result.warnings.push({
          category: 'Test Coverage',
          message: `Coverage ${totalCoverage}% is below recommended ${warnCoverage}%`,
          suggestion: 'Consider adding more tests to improve coverage'
        });
      }
      
      // Check critical paths
      const criticalPaths = this.policies.coverage.critical_paths;
      for (const path of criticalPaths) {
        const files = glob.sync(path);
        const changedCriticalFiles = files.filter(f => 
          this.changedFiles.includes(f)
        );
        
        for (const file of changedCriticalFiles) {
          const fileCoverage = this.getFileCoverage(file, coverageSummary);
          if (fileCoverage < this.policies.coverage.critical_paths_threshold) {
            result.errors.push({
              category: 'Critical Path Coverage',
              message: `Critical file ${file} has only ${fileCoverage}% coverage`,
              file: file,
              severity: 'high'
            });
          }
        }
      }
      
      // Check if changed files have tests
      const sourceFiles = this.changedFiles.filter(f => 
        f.startsWith('src/') && 
        !f.includes('.test.') && 
        !f.includes('.spec.')
      );
      
      for (const file of sourceFiles) {
        if (!this.hasTestFile(file)) {
          result.errors.push({
            category: 'Missing Tests',
            message: `No test file found for ${file}`,
            file: file,
            severity: 'high'
          });
        }
      }
      
      console.log(`  ✅ Overall coverage: ${totalCoverage}%\n`);
      
    } catch (error) {
      result.blockers.push({
        category: 'Test Coverage',
        message: 'Failed to generate coverage report',
        required_action: 'Ensure tests can run successfully'
      });
    }
  }
  
  private async validateSecurity(result: ValidationResult) {
    console.log('🔒 Scanning for security issues...');
    
    // 1. Check for vulnerable patterns in code
    const vulnerablePatterns = this.policies.security.vulnerable_patterns;
    
    for (const file of this.changedFiles) {
      if (!file.endsWith('.ts') && !file.endsWith('.js')) continue;
      
      const content = readFileSync(file, 'utf8');
      
      for (const pattern of vulnerablePatterns) {
        if (content.includes(pattern)) {
          result.errors.push({
            category: 'Security',
            message: `Dangerous pattern "${pattern}" found`,
            file: file,
            severity: 'high'
          });
          result.summary.security_issues++;
        }
      }
      
      // Check for hardcoded secrets
      const secretPatterns = [
        /password\s*=\s*["'][^"']+["']/i,
        /api[_-]?key\s*=\s*["'][^"']+["']/i,
        /secret\s*=\s*["'][^"']+["']/i,
        /token\s*=\s*["'][^"']+["']/i
      ];
      
      for (const pattern of secretPatterns) {
        if (pattern.test(content)) {
          result.blockers.push({
            category: 'Security',
            message: `Possible hardcoded secret in ${file}`,
            required_action: 'Use environment variables or secret management'
          });
          result.summary.security_issues++;
        }
      }
    }
    
    // 2. Check dependencies for vulnerabilities
    try {
      const auditOutput = execSync('npm audit --json', { 
        encoding: 'utf8',
        stdio: 'pipe'
      });
      
      const audit = JSON.parse(auditOutput);
      
      if (audit.metadata.vulnerabilities.high > 0) {
        result.blockers.push({
          category: 'Security',
          message: `${audit.metadata.vulnerabilities.high} high severity vulnerabilities found`,
          required_action: 'Run npm audit fix or update vulnerable packages'
        });
        result.summary.security_issues += audit.metadata.vulnerabilities.high;
      }
      
      if (audit.metadata.vulnerabilities.moderate > 0) {
        result.warnings.push({
          category: 'Security',
          message: `${audit.metadata.vulnerabilities.moderate} moderate vulnerabilities found`,
          suggestion: 'Consider updating packages: npm audit fix'
        });
      }
      
    } catch (error) {
      // npm audit exits with error code if vulnerabilities found
      result.warnings.push({
        category: 'Security',
        message: 'Dependency vulnerabilities detected',
        suggestion: 'Run npm audit for details'
      });
    }
    
    // 3. Check for blocked packages
    if (existsSync('package.json')) {
      const packageJson = JSON.parse(readFileSync('package.json', 'utf8'));
      const allDeps = {
        ...packageJson.dependencies,
        ...packageJson.devDependencies
      };
      
      const blockedPackages = this.policies.security.blocked_packages;
      
      for (const pkg of blockedPackages) {
        if (allDeps[pkg]) {
          result.errors.push({
            category: 'Security',
            message: `Blocked package "${pkg}" is being used`,
            severity: 'medium'
          });
        }
      }
    }
    
    console.log(`  ${result.summary.security_issues > 0 ? '❌' : '✅'} Security issues: ${result.summary.security_issues}\n`);
  }
  
  private async validateBreakingChanges(result: ValidationResult) {
    console.log('💥 Checking for breaking changes...');
    
    const breakingChanges: string[] = [];
    
    // 1. Check API changes
    const apiFiles = this.changedFiles.filter(f => 
      f.includes('controller') || f.includes('routes')
    );
    
    if (apiFiles.length > 0) {
      result.warnings.push({
        category: 'Breaking Changes',
        message: 'API files modified - verify backward compatibility',
        suggestion: 'Check if API changes are backward compatible'
      });
      
      // Require migration plan for API changes
      if (!this.hasFile('docs/migration-guide.md')) {
        result.errors.push({
          category: 'Breaking Changes',
          message: 'API changes detected but no migration guide found',
          severity: 'medium'
        });
        breakingChanges.push('API modification');
      }
    }
    
    // 2. Check database schema changes
    const migrationFiles = this.changedFiles.filter(f => 
      f.includes('migration') || f.includes('schema')
    );
    
    if (migrationFiles.length > 0) {
      breakingChanges.push('Database schema');
      
      result.warnings.push({
        category: 'Breaking Changes',
        message: 'Database schema changes detected',
        suggestion: 'Ensure migrations are backward compatible and reversible'
      });
      
      // Check for rollback migration
      for (const migration of migrationFiles) {
        const content = readFileSync(migration, 'utf8');
        if (!content.includes('down') && !content.includes('revert')) {
          result.errors.push({
            category: 'Breaking Changes',
            message: `Migration ${migration} missing rollback`,
            file: migration,
            severity: 'high'
          });
        }
      }
    }
    
    // 3. Check environment variable changes
    if (this.changedFiles.includes('.env.example')) {
      const diff = this.getFileDiff('.env.example');
      const addedVars = diff.filter(line => line.startsWith('+')).length;
      const removedVars = diff.filter(line => line.startsWith('-')).length;
      
      if (removedVars > 0) {
        result.errors.push({
          category: 'Breaking Changes',
          message: `${removedVars} environment variables removed`,
          file: '.env.example',
          severity: 'high'
        });
        breakingChanges.push('Environment variables');
      }
      
      if (addedVars > 0) {
        result.warnings.push({
          category: 'Breaking Changes',
          message: `${addedVars} new environment variables added`,
          suggestion: 'Update deployment documentation'
        });
      }
    }
    
    // 4. Check dependency major version updates
    if (this.changedFiles.includes('package.json')) {
      const majorUpdates = this.detectMajorDependencyUpdates();
      
      if (majorUpdates.length > 0) {
        result.warnings.push({
          category: 'Breaking Changes',
          message: `Major dependency updates: ${majorUpdates.join(', ')}`,
          suggestion: 'Review changelogs and test thoroughly'
        });
        breakingChanges.push('Dependencies');
      }
    }
    
    result.summary.breaking_changes = breakingChanges.length;
    
    if (breakingChanges.length > 0 && !this.hasFile('CHANGELOG.md')) {
      result.blockers.push({
        category: 'Breaking Changes',
        message: `Breaking changes detected: ${breakingChanges.join(', ')}`,
        required_action: 'Update CHANGELOG.md with breaking changes section'
      });
    }
    
    console.log(`  ${breakingChanges.length > 0 ? '⚠️' : '✅'} Breaking changes: ${breakingChanges.length}\n`);
  }
  
  private async validateDocumentation(result: ValidationResult) {
    console.log('📚 Checking documentation...');
    
    const missingDocs: string[] = [];
    
    // 1. Check README updates for new features
    const featureFiles = this.changedFiles.filter(f => 
      f.startsWith('src/') && !f.includes('.test.')
    );
    
    if (featureFiles.length > 0 && !this.changedFiles.includes('README.md')) {
      result.warnings.push({
        category: 'Documentation',
        message: 'Source files changed but README not updated',
        suggestion: 'Update README with new features or changes'
      });
      missingDocs.push('README');
    }
    
    // 2. Check API documentation
    const controllerFiles = this.changedFiles.filter(f => 
      f.includes('controller')
    );
    
    if (controllerFiles.length > 0) {
      for (const file of controllerFiles) {
        const content = readFileSync(file, 'utf8');
        
        // Check for OpenAPI/Swagger comments
        if (!content.includes('@swagger') && !content.includes('@openapi')) {
          result.errors.push({
            category: 'Documentation',
            message: 'API endpoint missing OpenAPI documentation',
            file: file,
            severity: 'medium'
          });
          missingDocs.push('API docs');
        }
      }
    }
    
    // 3. Check JSDoc for public functions
    const tsFiles = this.changedFiles.filter(f => 
      f.endsWith('.ts') && !f.endsWith('.test.ts')
    );
    
    for (const file of tsFiles) {
      const content = readFileSync(file, 'utf8');
      const exportedFunctions = content.match(/export (async )?function \w+/g) || [];
      const jsdocComments = content.match(/\/\*\*[\s\S]*?\*\//g) || [];
      
      if (exportedFunctions.length > jsdocComments.length) {
        result.warnings.push({
          category: 'Documentation',
          message: `${file} has exported functions without JSDoc`,
          suggestion: 'Add JSDoc comments to public APIs'
        });
      }
    }
    
    // 4. Check CHANGELOG for changes
    if (this.changedFiles.length > 0 && !this.changedFiles.includes('CHANGELOG.md')) {
      result.errors.push({
        category: 'Documentation',
        message: 'CHANGELOG.md not updated',
        severity: 'low'
      });
      missingDocs.push('CHANGELOG');
    }
    
    result.summary.missing_documentation = missingDocs.length;
    
    console.log(`  ${missingDocs.length > 0 ? '⚠️' : '✅'} Missing documentation: ${missingDocs.length}\n`);
  }
  
  private async validateCodeQuality(result: ValidationResult) {
    console.log('✨ Checking code quality...');
    
    try {
      // Run linter
      execSync('npm run lint', { stdio: 'pipe' });
      console.log('  ✅ Linting passed\n');
    } catch (error) {
      result.errors.push({
        category: 'Code Quality',
        message: 'Linting failed',
        severity: 'high'
      });
      console.log('  ❌ Linting failed\n');
    }
    
    try {
      // Run type checking
      execSync('npm run type-check', { stdio: 'pipe' });
      console.log('  ✅ Type checking passed\n');
    } catch (error) {
      result.errors.push({
        category: 'Code Quality',
        message: 'Type checking failed',
        severity: 'high'
      });
      console.log('  ❌ Type checking failed\n');
    }
  }
  
  private async validateDependencies(result: ValidationResult) {
    console.log('📦 Validating dependencies...');
    
    if (this.changedFiles.includes('package.json')) {
      const diff = this.getFileDiff('package.json');
      
      // Check for package-lock.json update
      if (!this.changedFiles.includes('package-lock.json')) {
        result.blockers.push({
          category: 'Dependencies',
          message: 'package.json changed but package-lock.json not updated',
          required_action: 'Run npm install to update lockfile'
        });
      }
      
      // Check for outdated dependencies
      try {
        const outdated = execSync('npm outdated --json', { 
          encoding: 'utf8',
          stdio: 'pipe'
        });
        
        if (outdated) {
          const packages = JSON.parse(outdated);
          const count = Object.keys(packages).length;
          
          if (count > 0) {
            result.warnings.push({
              category: 'Dependencies',
              message: `${count} outdated dependencies found`,
              suggestion: 'Consider updating outdated packages'
            });
          }
        }
      } catch (error) {
        // No outdated packages
      }
    }
    
    console.log('  ✅ Dependencies validated\n');
  }
  
  // Helper methods
  private getFileCoverage(file: string, coverageSummary: any): number {
    const normalizedPath = file.replace(/\\/g, '/');
    if (coverageSummary[normalizedPath]) {
      return coverageSummary[normalizedPath].lines.pct;
    }
    return 0;
  }
  
  private hasTestFile(sourceFile: string): boolean {
    const testFile1 = sourceFile.replace('.ts', '.test.ts');
    const testFile2 = sourceFile.replace('.ts', '.spec.ts');
    const testFile3 = sourceFile.replace('src/', 'tests/').replace('.ts', '.test.ts');
    
    return existsSync(testFile1) || existsSync(testFile2) || existsSync(testFile3);
  }
  
  private hasFile(path: string): boolean {
    return existsSync(path) || this.changedFiles.includes(path);
  }
  
  private getFileDiff(file: string): string[] {
    try {
      const diff = execSync(`git diff HEAD origin/main -- ${file}`, {
        encoding: 'utf8'
      });
      return diff.split('\n');
    } catch (error) {
      return [];
    }
  }
  
  private detectMajorDependencyUpdates(): string[] {
    try {
      const diff = this.getFileDiff('package.json');
      const updates: string[] = [];
      
      for (const line of diff) {
        if (line.startsWith('-') && line.includes('"')) {
          const pkg = line.match(/"([^"]+)":\s*"([^"]+)"/);
          if (pkg) {
            const [, name, oldVersion] = pkg;
            const newVersionLine = diff.find(l => 
              l.startsWith('+') && l.includes(`"${name}"`)
            );
            
            if (newVersionLine) {
              const newPkg = newVersionLine.match(/"([^"]+)":\s*"([^"]+)"/);
              if (newPkg) {
                const [, , newVersion] = newPkg;
                
                // Check if major version changed
                const oldMajor = oldVersion.split('.')[0].replace(/\D/g, '');
                const newMajor = newVersion.split('.')[0].replace(/\D/g, '');
                
                if (oldMajor !== newMajor) {
                  updates.push(`${name} (${oldVersion} → ${newVersion})`);
                }
              }
            }
          }
        }
      }
      
      return updates;
    } catch (error) {
      return [];
    }
  }
  
  generateReport(result: ValidationResult): string {
    let report = '\n';
    report += '╔════════════════════════════════════════════════════════════════╗\n';
    report += '║          PR GOVERNANCE VALIDATION REPORT                      ║\n';
    report += '╚════════════════════════════════════════════════════════════════╝\n\n';
    
    // Summary
    report += '📊 SUMMARY\n';
    report += '─────────────────────────────────────────────────────────────────\n';
    report += `Files Changed: ${result.summary.total_files_changed}\n`;
    report += `Test Coverage: ${result.summary.test_coverage.toFixed(2)}%\n`;
    report += `Security Issues: ${result.summary.security_issues}\n`;
    report += `Breaking Changes: ${result.summary.breaking_changes}\n`;
    report += `Missing Documentation: ${result.summary.missing_documentation}\n\n`;
    
    // Blockers
    if (result.blockers.length > 0) {
      report += '🚫 BLOCKERS (Must Fix)\n';
      report += '─────────────────────────────────────────────────────────────────\n';
      result.blockers.forEach((blocker, i) => {
        report += `${i + 1}. [${blocker.category}] ${blocker.message}\n`;
        report += `   ➜ Action Required: ${blocker.required_action}\n\n`;
      });
    }
    
    // Errors
    if (result.errors.length > 0) {
      report += '❌ ERRORS (High Priority)\n';
      report += '─────────────────────────────────────────────────────────────────\n';
      result.errors.forEach((error, i) => {
        report += `${i + 1}. [${error.category}] ${error.message}\n`;
        if (error.file) {
          report += `   File: ${error.file}\n`;
        }
        report += `   Severity: ${error.severity}\n\n`;
      });
    }
    
    // Warnings
    if (result.warnings.length > 0) {
      report += '⚠️  WARNINGS (Recommended)\n';
      report += '─────────────────────────────────────────────────────────────────\n';
      result.warnings.forEach((warning, i) => {
        report += `${i + 1}. [${warning.category}] ${warning.message}\n`;
        report += `   Suggestion: ${warning.suggestion}\n\n`;
      });
    }
    
    // Final verdict
    report += '═════════════════════════════════════════════════════════════════\n';
    if (result.passed) {
      report += '✅ PR VALIDATION PASSED - Ready to merge\n';
    } else {
      report += '❌ PR VALIDATION FAILED - Fix blockers before merging\n';
      report += `\nBlockers to resolve: ${result.blockers.length}\n`;
    }
    report += '═════════════════════════════════════════════════════════════════\n\n';
    
    return report;
  }
}

// Main execution
async function main() {
  const validator = new PRGovernanceValidator();
  const result = await validator.validate();
  const report = validator.generateReport(result);
  
  console.log(report);
  
  // Save report
  writeFileSync('.github/pr-validation-report.md', report);
  
  // Exit with error code if validation failed
  if (!result.passed) {
    process.exit(1);
  }
}

main().catch(error => {
  console.error('Validation failed:', error);
  process.exit(1);
});
```

### Package.json Scripts
```json
{
  "scripts": {
    "pr-check": "ts-node .github/scripts/pre-push-validation.ts",
    "pre-push": "npm run pr-check"
  }
}
```

### Git Pre-push Hook
```bash
#!/bin/bash
# .git/hooks/pre-push

echo "🔍 Running PR governance checks..."

npm run pr-check

if [ $? -ne 0 ]; then
    echo ""
    echo "❌ PR validation failed. Push aborted."
    echo "   Fix the issues above or use --no-verify to skip checks."
    exit 1
fi

echo "✅ All checks passed!"
exit 0
```