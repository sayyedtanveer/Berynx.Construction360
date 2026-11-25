export type Field = {
  key: string;
  label: string;
  type?: string;
  options?: string[];
  required?: boolean;
};
export type Group = { key: string; title: string; fields: Field[]; repeat?: boolean };
const field = (key: string, label: string, type = 'text', required = true): Field => ({
  key,
  label,
  type,
  required,
});
const choice = (key: string, label: string, options: string[]): Field => ({
  key,
  label,
  options,
  required: true,
});
export const documents = [
  'Quotation',
  'SalesOrder',
  'PurchaseOrder',
  'Challan',
  'Invoice',
  'Receipt',
];
export const steps = [
  'company',
  'locations',
  'numbering',
  'accounts',
  'taxes',
  'invites',
  'openings',
  'checklist',
];
export const titles: Record<string, string> = {
  platform: 'Platform tenants',
  create: 'Create tenant',
  plans: 'Plans and module catalogue',
  wizard: 'Onboarding',
  company: 'Company profile',
  locations: 'Registrations, branches and plants',
  numbering: 'Financial year and numbering',
  accounts: 'Bank, UPI and cash accounts',
  taxes: 'Tax defaults and units',
  invites: 'Invite users',
  openings: 'Opening balances',
  checklist: 'Go-live checklist',
  usage: 'Plan and usage',
};
export const forms: Record<string, Group[]> = {
  company: [
    {
      key: 'company',
      title: 'Registered company',
      fields: [
        field('legalName', 'Legal name'),
        field('tradeName', 'Trade name', 'text', false),
        field('pan', 'PAN'),
        field('address', 'Registered address'),
        field('logo', 'Logo URL', 'url', false),
        field('msme', 'MSME registered', 'checkbox', false),
        field('udyam', 'Udyam registration', 'text', false),
        field('eInvoice', 'E-invoice applicable', 'checkbox', false),
        field('eWayBill', 'E-way bill applicable', 'checkbox', false),
        field('paymentTerms', 'Default payment terms', 'text', false),
      ],
    },
  ],
  locations: [
    {
      key: 'registrations',
      title: 'GST registrations',
      repeat: true,
      fields: [field('gstin', 'GSTIN')],
    },
    {
      key: 'branches',
      title: 'Branches',
      repeat: true,
      fields: [
        field('name', 'Branch name'),
        field('address', 'Branch address'),
        field('gstin', 'Registration', 'registration'),
      ],
    },
    {
      key: 'plants',
      title: 'Plants',
      repeat: true,
      fields: [
        field('name', 'Plant name'),
        field('address', 'Plant address'),
        field('branchId', 'Branch', 'branch'),
      ],
    },
  ],
  numbering: [
    {
      key: 'fiscal',
      title: 'Financial year',
      fields: [field('startMonth', 'Starting month (1–12)', 'number')],
    },
    {
      key: 'series',
      title: 'Branch document series',
      repeat: true,
      fields: [
        field('branchId', 'Branch', 'branch'),
        choice('document', 'Document', documents),
        field('prefix', 'Prefix', 'text', false),
        field('branchCode', 'Branch code', 'text', false),
        field('pattern', 'Pattern, e.g. {PREFIX}/{FY}/{SEQ:4}'),
      ],
    },
  ],
  accounts: [
    {
      key: 'accounts',
      title: 'Accounts',
      repeat: true,
      fields: [
        field('name', 'Account name'),
        choice('kind', 'Type', ['Cash', 'Bank']),
        field('upi', 'UPI ID', 'text', false),
        field('accountNumber', 'Bank account number', 'text', false),
        field('ifsc', 'IFSC', 'text', false),
      ],
    },
  ],
  taxes: [
    {
      key: 'taxes',
      title: 'Tax defaults',
      repeat: true,
      fields: [
        field('name', 'Tax name'),
        field('rate', 'Rate (%)', 'number'),
        field('unit', 'Unit'),
      ],
    },
  ],
  invites: [
    {
      key: 'invite',
      title: 'Additional user',
      fields: [
        field('email', 'Email address', 'email', false),
        field('skip', 'Explicitly skip invitation', 'checkbox', false),
      ],
    },
  ],
  openings: [
    {
      key: 'rows',
      title: 'Ledger and bill entries',
      repeat: true,
      fields: [
        choice('kind', 'Type', ['Bank', 'Cash', 'Customer', 'Supplier', 'Ledger']),
        field('ledger', 'Ledger or party'),
        field('bill', 'Bill reference', 'text', false),
        field('debit', 'Debit', 'text', false),
        field('credit', 'Credit', 'text', false),
      ],
    },
  ],
  create: [
    {
      key: 'tenant',
      title: 'Tenant, first administrator and licence',
      fields: [
        field('name', 'Tenant name'),
        choice('plan', 'Base plan', []),
        field('adminEmail', 'First admin email', 'email'),
        { key: 'addOnModules', label: 'Optional module add-ons', type: 'multi-select', options: [], required: false },
      ],
    },
  ],
};
