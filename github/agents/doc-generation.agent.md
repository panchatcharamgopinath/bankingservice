# Word Document Generator Agent

## Role
You are a technical documentation specialist responsible for generating Microsoft Word documents from requirements and specifications. This agent extends the requirements-planner to produce formatted Word documents in addition to markdown.

## Responsibilities
- Convert markdown specifications to Word format
- Apply professional formatting and styling
- Include diagrams, tables, and visual elements
- Generate table of contents and cross-references
- Create document templates for consistency
- Export to .docx format

## Input Artifacts
- Markdown specifications from `/docs/specs/`
- Backlog stories from `/backlog/`
- Design documents from `/design/`
- Requirements from requirements-planner agent

## Output Artifacts
- Word documents in `/docs/word/`
- Document templates in `/templates/word/`
- PDF exports (optional)

## Dependencies

### Node.js Package for Word Generation
```json
{
  "dependencies": {
    "docx": "^8.5.0",
    "markdown-it": "^13.0.2",
    "mammoth": "^1.6.0"
  }
}
```

## Word Document Generator Implementation

### Main Generator Class
```typescript
import {
  Document,
  Packer,
  Paragraph,
  TextRun,
  HeadingLevel,
  AlignmentType,
  Table,
  TableRow,
  TableCell,
  WidthType,
  BorderStyle,
  convertInchesToTwip,
  UnderlineType,
  TableOfContents
} from 'docx';
import { writeFile } from 'fs/promises';
import MarkdownIt from 'markdown-it';

export class WordDocumentGenerator {
  private md: MarkdownIt;

  constructor() {
    this.md = new MarkdownIt();
  }

  async generateSpecificationDocument(
    specData: SpecificationData
  ): Promise<void> {
    const doc = new Document({
      creator: 'AI SDLC System',
      description: 'Technical Specification Document',
      title: specData.title,
      sections: [
        {
          properties: {},
          children: [
            // Cover Page
            ...this.createCoverPage(specData),
            
            // Table of Contents
            new TableOfContents('Table of Contents', {
              hyperlink: true,
              headingStyleRange: '1-3'
            }),
            
            new Paragraph({ text: '', pageBreakBefore: true }),
            
            // Document Sections
            ...this.createExecutiveSummary(specData),
            ...this.createRequirements(specData),
            ...this.createArchitecture(specData),
            ...this.createImplementationPlan(specData),
            ...this.createAppendix(specData)
          ]
        }
      ]
    });

    const buffer = await Packer.toBuffer(doc);
    await writeFile(
      `./docs/word/${specData.filename}.docx`,
      buffer
    );
  }

  private createCoverPage(data: SpecificationData): Paragraph[] {
    return [
      new Paragraph({
        text: data.title,
        heading: HeadingLevel.TITLE,
        alignment: AlignmentType.CENTER,
        spacing: { before: 400, after: 200 }
      }),
      new Paragraph({
        text: data.subtitle || '',
        alignment: AlignmentType.CENTER,
        spacing: { after: 400 }
      }),
      new Paragraph({
        children: [
          new TextRun({
            text: 'Version: ',
            bold: true
          }),
          new TextRun(data.version || '1.0')
        ],
        alignment: AlignmentType.CENTER,
        spacing: { after: 100 }
      }),
      new Paragraph({
        children: [
          new TextRun({
            text: 'Date: ',
            bold: true
          }),
          new TextRun(new Date().toLocaleDateString())
        ],
        alignment: AlignmentType.CENTER,
        spacing: { after: 100 }
      }),
      new Paragraph({
        text: '',
        pageBreakBefore: true
      })
    ];
  }

  private createExecutiveSummary(
    data: SpecificationData
  ): Paragraph[] {
    return [
      new Paragraph({
        text: 'Executive Summary',
        heading: HeadingLevel.HEADING_1,
        spacing: { before: 240, after: 120 }
      }),
      new Paragraph({
        text: data.executiveSummary,
        spacing: { after: 120 }
      }),
      
      // Key Objectives
      new Paragraph({
        text: 'Key Objectives',
        heading: HeadingLevel.HEADING_2,
        spacing: { before: 200, after: 120 }
      }),
      ...data.objectives.map(
        (obj, index) =>
          new Paragraph({
            text: `${index + 1}. ${obj}`,
            spacing: { after: 60 },
            indent: { left: convertInchesToTwip(0.5) }
          })
      )
    ];
  }

  private createRequirements(
    data: SpecificationData
  ): Paragraph[] {
    const paragraphs: Paragraph[] = [
      new Paragraph({
        text: 'Requirements',
        heading: HeadingLevel.HEADING_1,
        spacing: { before: 240, after: 120 },
        pageBreakBefore: true
      })
    ];

    // Functional Requirements
    paragraphs.push(
      new Paragraph({
        text: 'Functional Requirements',
        heading: HeadingLevel.HEADING_2,
        spacing: { before: 200, after: 120 }
      })
    );

    data.functionalRequirements.forEach((req, index) => {
      paragraphs.push(
        new Paragraph({
          text: `FR-${String(index + 1).padStart(3, '0')}: ${req.title}`,
          heading: HeadingLevel.HEADING_3,
          spacing: { before: 120, after: 60 }
        }),
        new Paragraph({
          text: req.description,
          spacing: { after: 60 }
        })
      );

      if (req.acceptanceCriteria) {
        paragraphs.push(
          new Paragraph({
            children: [
              new TextRun({ text: 'Acceptance Criteria:', bold: true })
            ],
            spacing: { after: 40 }
          }),
          ...req.acceptanceCriteria.map(
            (ac) =>
              new Paragraph({
                text: `• ${ac}`,
                indent: { left: convertInchesToTwip(0.5) },
                spacing: { after: 20 }
              })
          )
        );
      }
    });

    // Non-Functional Requirements
    paragraphs.push(
      new Paragraph({
        text: 'Non-Functional Requirements',
        heading: HeadingLevel.HEADING_2,
        spacing: { before: 200, after: 120 }
      })
    );

    const nfrTable = this.createNFRTable(
      data.nonFunctionalRequirements
    );
    paragraphs.push(nfrTable);

    return paragraphs;
  }

  private createNFRTable(nfrs: NonFunctionalRequirement[]): Table {
    return new Table({
      width: {
        size: 100,
        type: WidthType.PERCENTAGE
      },
      rows: [
        // Header Row
        new TableRow({
          children: [
            new TableCell({
              children: [
                new Paragraph({
                  text: 'Category',
                  bold: true
                })
              ],
              shading: { fill: '4472C4' }
            }),
            new TableCell({
              children: [
                new Paragraph({
                  text: 'Requirement',
                  bold: true
                })
              ],
              shading: { fill: '4472C4' }
            }),
            new TableCell({
              children: [
                new Paragraph({
                  text: 'Target',
                  bold: true
                })
              ],
              shading: { fill: '4472C4' }
            })
          ]
        }),
        // Data Rows
        ...nfrs.map(
          (nfr) =>
            new TableRow({
              children: [
                new TableCell({
                  children: [new Paragraph(nfr.category)]
                }),
                new TableCell({
                  children: [new Paragraph(nfr.requirement)]
                }),
                new TableCell({
                  children: [new Paragraph(nfr.target)]
                })
              ]
            })
        )
      ]
    });
  }

  private createArchitecture(
    data: SpecificationData
  ): Paragraph[] {
    return [
      new Paragraph({
        text: 'System Architecture',
        heading: HeadingLevel.HEADING_1,
        spacing: { before: 240, after: 120 },
        pageBreakBefore: true
      }),
      new Paragraph({
        text: 'Architecture Overview',
        heading: HeadingLevel.HEADING_2,
        spacing: { before: 200, after: 120 }
      }),
      new Paragraph({
        text: data.architecture.overview,
        spacing: { after: 120 }
      }),
      
      // Components
      new Paragraph({
        text: 'System Components',
        heading: HeadingLevel.HEADING_2,
        spacing: { before: 200, after: 120 }
      }),
      ...data.architecture.components.map((comp) => [
        new Paragraph({
          text: comp.name,
          heading: HeadingLevel.HEADING_3,
          spacing: { before: 120, after: 60 }
        }),
        new Paragraph({
          text: comp.description,
          spacing: { after: 40 }
        }),
        new Paragraph({
          children: [
            new TextRun({ text: 'Technologies: ', bold: true }),
            new TextRun(comp.technologies.join(', '))
          ],
          spacing: { after: 60 }
        })
      ]).flat()
    ];
  }

  private createImplementationPlan(
    data: SpecificationData
  ): Paragraph[] {
    const paragraphs: Paragraph[] = [
      new Paragraph({
        text: 'Implementation Plan',
        heading: HeadingLevel.HEADING_1,
        spacing: { before: 240, after: 120 },
        pageBreakBefore: true
      })
    ];

    // Timeline Table
    const timelineTable = new Table({
      width: { size: 100, type: WidthType.PERCENTAGE },
      rows: [
        new TableRow({
          children: [
            new TableCell({
              children: [
                new Paragraph({ text: 'Phase', bold: true })
              ],
              shading: { fill: '70AD47' }
            }),
            new TableCell({
              children: [
                new Paragraph({ text: 'Duration', bold: true })
              ],
              shading: { fill: '70AD47' }
            }),
            new TableCell({
              children: [
                new Paragraph({ text: 'Deliverables', bold: true })
              ],
              shading: { fill: '70AD47' }
            })
          ]
        }),
        ...data.implementationPlan.phases.map(
          (phase) =>
            new TableRow({
              children: [
                new TableCell({
                  children: [new Paragraph(phase.name)]
                }),
                new TableCell({
                  children: [new Paragraph(phase.duration)]
                }),
                new TableCell({
                  children: [
                    new Paragraph(phase.deliverables.join(', '))
                  ]
                })
              ]
            })
        )
      ]
    });

    paragraphs.push(timelineTable);

    return paragraphs;
  }

  private createAppendix(data: SpecificationData): Paragraph[] {
    return [
      new Paragraph({
        text: 'Appendix',
        heading: HeadingLevel.HEADING_1,
        spacing: { before: 240, after: 120 },
        pageBreakBefore: true
      }),
      new Paragraph({
        text: 'A. Glossary',
        heading: HeadingLevel.HEADING_2,
        spacing: { before: 200, after: 120 }
      }),
      ...data.glossary.map(
        (term) =>
          new Paragraph({
            children: [
              new TextRun({ text: `${term.term}: `, bold: true }),
              new TextRun(term.definition)
            ],
            spacing: { after: 60 }
          })
      )
    ];
  }
}

// Type definitions
interface SpecificationData {
  title: string;
  subtitle?: string;
  version?: string;
  filename: string;
  executiveSummary: string;
  objectives: string[];
  functionalRequirements: FunctionalRequirement[];
  nonFunctionalRequirements: NonFunctionalRequirement[];
  architecture: Architecture;
  implementationPlan: ImplementationPlan;
  glossary: GlossaryTerm[];
}

interface FunctionalRequirement {
  title: string;
  description: string;
  acceptanceCriteria?: string[];
}

interface NonFunctionalRequirement {
  category: string;
  requirement: string;
  target: string;
}

interface Architecture {
  overview: string;
  components: Component[];
}

interface Component {
  name: string;
  description: string;
  technologies: string[];
}

interface ImplementationPlan {
  phases: Phase[];
}

interface Phase {
  name: string;
  duration: string;
  deliverables: string[];
}

interface GlossaryTerm {
  term: string;
  definition: string;
}
```

### Usage Script
```typescript
// scripts/generate-word-doc.ts
import { WordDocumentGenerator } from './word-generator';
import { readFile } from 'fs/promises';
import { parse } from 'yaml';

async function main() {
  const generator = new WordDocumentGenerator();
  
  // Read specification data from markdown or YAML
  const specContent = await readFile(
    './docs/specs/event-management-dynamics-crm-marketing-app-spec.md',
    'utf-8'
  );
  
  // Parse and convert to structured data
  const specData = parseSpecification(specContent);
  
  // Generate Word document
  await generator.generateSpecificationDocument(specData);
  
  console.log('✅ Word document generated successfully');
}

function parseSpecification(content: string): SpecificationData {
  // Parse markdown content and extract structured data
  // This is a simplified example
  return {
    title: 'Event Management System Specification',
    subtitle: 'Dynamics CRM Marketing Application',
    version: '1.0',
    filename: 'event-management-spec',
    executiveSummary: 'This document outlines...',
    objectives: [
      'Implement event registration system',
      'Integrate with Dynamics CRM',
      'Enable marketing automation'
    ],
    functionalRequirements: [
      {
        title: 'Event Registration',
        description: 'Users can register for events...',
        acceptanceCriteria: [
          'Registration form validation',
          'Email confirmation sent',
          'CRM integration successful'
        ]
      }
    ],
    nonFunctionalRequirements: [
      {
        category: 'Performance',
        requirement: 'Page load time',
        target: '< 2 seconds'
      },
      {
        category: 'Scalability',
        requirement: 'Concurrent users',
        target: '1000+ users'
      }
    ],
    architecture: {
      overview: 'The system follows a microservices architecture...',
      components: [
        {
          name: 'Frontend',
          description: 'React-based user interface',
          technologies: ['React', 'TypeScript', 'Material-UI']
        },
        {
          name: 'Backend API',
          description: 'Node.js REST API',
          technologies: ['Node.js', 'Express', 'TypeORM']
        }
      ]
    },
    implementationPlan: {
      phases: [
        {
          name: 'Phase 1: Foundation',
          duration: '2 weeks',
          deliverables: ['Database schema', 'API skeleton', 'CI/CD setup']
        },
        {
          name: 'Phase 2: Core Features',
          duration: '4 weeks',
          deliverables: ['Event management', 'User registration', 'CRM integration']
        }
      ]
    },
    glossary: [
      { term: 'CRM', definition: 'Customer Relationship Management' },
      { term: 'API', definition: 'Application Programming Interface' }
    ]
  };
}

main().catch(console.error);
```

### Package.json Script
```json
{
  "scripts": {
    "generate:word": "ts-node scripts/generate-word-doc.ts",
    "generate:word:all": "ts-node scripts/generate-all-word-docs.ts"
  }
}
```

## Document Templates

### Professional Template with Styles
```typescript
const createStyledDocument = (data: SpecificationData) => {
  return new Document({
    styles: {
      paragraphStyles: [
        {
          id: 'CustomHeading1',
          name: 'Custom Heading 1',
          basedOn: 'Heading1',
          next: 'Normal',
          run: {
            size: 32,
            bold: true,
            color: '2E5090',
            font: 'Calibri'
          },
          paragraph: {
            spacing: { before: 240, after: 120 }
          }
        },
        {
          id: 'CustomHeading2',
          name: 'Custom Heading 2',
          basedOn: 'Heading2',
          run: {
            size: 28,
            bold: true,
            color: '4472C4',
            font: 'Calibri'
          },
          paragraph: {
            spacing: { before: 200, after: 100 }
          }
        }
      ]
    },
    // ... rest of document
  });
};
```

## Communication Protocol
After Word document generation:
```json
{
  "phase": "word-document-generation",
  "status": "complete",
  "artifacts": [
    "/docs/word/event-management-spec.docx",
    "/docs/word/requirements-document.docx"
  ],
  "metadata": {
    "pages": 45,
    "sections": 8,
    "tables": 12,
    "format": "docx"
  },
  "next_agent": "code-generation"
}
```