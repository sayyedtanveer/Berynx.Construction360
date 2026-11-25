import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { catchError, of } from 'rxjs';
import { API_BASE_URL } from '../../core/api/api-base-url.token';
import { ApiClient } from '../../core/api/api-client.service';
import { M03CreateInContextDrawerComponent, M03ContextResource } from './m03-create-in-context-drawer.component';

type FieldType = 'text' | 'number' | 'date' | 'email' | 'checkbox' | 'json' | 'select';
interface MasterField {
  key: string;
  label: string;
  type?: FieldType;
  required?: boolean;
  options?: string[];
  min?: number;
  contextResource?: M03ContextResource;
}
interface MasterConfig {
  title: string;
  endpoint: string;
  createEndpoint?: string;
  updateResource?: string;
  updateIdField?: string;
  deactivateResource?: string;
  deactivateIdField?: string;
  fields: MasterField[];
  columns: string[];
  importType?: string;
}

interface Customer360Response {
  party: Record<string, unknown>;
  sites: Record<string, unknown>[];
  contacts: Record<string, unknown>[];
  pricingAgreements: Record<string, unknown>[];
  activity: Record<string, unknown>[];
  activityAvailable: boolean;
  creditDecision: { status: string; reasons: string[]; canRequestOverride: boolean };
  creditFacts: {
    available: boolean;
    outstanding: number | null;
    overdue: number | null;
    unavailableReason: string | null;
  };
  metrics: {
    available: boolean;
    outstanding: number | null;
    overdue: number | null;
    creditExposure: number | null;
    lastOrderAt: string | null;
    ytdSales: number | null;
    ytdProfit: number | null;
    unavailableReason: string | null;
  };
  linkedDocuments: { available: boolean; message: string };
  unavailableIntegrations: string[];
}

const COMMON_PARTY_FIELDS: MasterField[] = [
  { key: 'legalName', label: 'Legal name', required: true },
  { key: 'tradeName', label: 'Trade name' },
  { key: 'gstin', label: 'GSTIN' },
  { key: 'pan', label: 'PAN' },
  { key: 'city', label: 'City' },
  { key: 'stateCode', label: 'GST state code' },
  { key: 'billingAddress', label: 'Billing address' },
  { key: 'salespersonUserId', label: 'Salesperson user ID' },
  { key: 'tagsJson', label: 'Tags (JSON array)', type: 'json' },
];

const CONFIGS: Record<string, MasterConfig> = {
  customers: {
    title: 'Customers',
    endpoint: '/m03/parties?role=Customer',
    createEndpoint: '/m03/parties',
    updateResource: '/m03/parties/:id',
    updateIdField: 'partyId',
    deactivateResource: '/m03/parties/:id/deactivate',
    deactivateIdField: 'partyId',
    fields: [
      ...COMMON_PARTY_FIELDS,
      { key: 'customerCategory', label: 'Customer category' },
      { key: 'customerGroup', label: 'Customer group' },
      { key: 'creditLimit', label: 'Credit limit', type: 'number', min: 0 },
      { key: 'creditDays', label: 'Credit days', type: 'number', min: 0 },
      { key: 'creditPolicy', label: 'Credit policy', type: 'select', options: ['Warn', 'SoftBlock', 'HardBlock'] },
      { key: 'creditStatus', label: 'Credit status', type: 'select', options: ['Active', 'OnHold', 'Blocked'] },
      { key: 'paymentTermId', label: 'Payment term ID' },
      { key: 'defaultPriceListId', label: 'Default price list ID' },
    ],
    columns: ['legalName', 'gstin', 'city', 'creditLimit', 'creditStatus'],
    importType: 'customers',
  },
  sites: {
    title: 'Customer sites',
    endpoint: '/m03/sites',
    createEndpoint: '/m03/sites',
    updateResource: '/m03/sites/:id',
    updateIdField: 'siteId',
    deactivateResource: '/m03/sites/:id/deactivate',
    deactivateIdField: 'siteId',
    fields: [
      { key: 'customerPartyId', label: 'Customer party', required: true, contextResource: 'customer' },
      { key: 'branchId', label: 'Branch ID' },
      { key: 'name', label: 'Site name', required: true },
      { key: 'address', label: 'Address', required: true },
      { key: 'city', label: 'City', required: true },
      { key: 'stateCode', label: 'GST state code', required: true },
      { key: 'gstin', label: 'Site GSTIN' },
      { key: 'latitude', label: 'Latitude', type: 'number' },
      { key: 'longitude', label: 'Longitude', type: 'number' },
      { key: 'defaultPlantId', label: 'Default plant ID' },
      { key: 'accessNotes', label: 'Access notes' },
    ],
    columns: ['name', 'customerPartyId', 'city', 'stateCode', 'status'],
    importType: 'sites',
  },
  contacts: {
    title: 'Contacts',
    endpoint: '/m03/contacts',
    createEndpoint: '/m03/contacts',
    updateResource: '/m03/contacts/:id',
    updateIdField: 'contactId',
    deactivateResource: '/m03/contacts/:id/deactivate',
    deactivateIdField: 'contactId',
    fields: [
      { key: 'partyId', label: 'Party', required: true, contextResource: 'customer' },
      { key: 'siteId', label: 'Customer site', contextResource: 'site' },
      { key: 'name', label: 'Contact name', required: true },
      { key: 'role', label: 'Role', required: true },
      { key: 'email', label: 'Email', type: 'email' },
      { key: 'mobile', label: 'Mobile (+country code)' },
      { key: 'emailConsent', label: 'Email consent', type: 'checkbox' },
      { key: 'smsConsent', label: 'SMS consent', type: 'checkbox' },
      { key: 'whatsAppConsent', label: 'WhatsApp consent', type: 'checkbox' },
      { key: 'consentSource', label: 'Consent source' },
    ],
    columns: ['name', 'role', 'email', 'mobile', 'emailConsent'],
  },
  suppliers: {
    title: 'Suppliers and plants',
    endpoint: '/m03/parties?role=Supplier',
    createEndpoint: '/m03/parties',
    updateResource: '/m03/parties/:id',
    updateIdField: 'partyId',
    deactivateResource: '/m03/parties/:id/deactivate',
    deactivateIdField: 'partyId',
    fields: [
      ...COMMON_PARTY_FIELDS,
      { key: 'isMsme', label: 'MSME / Udyam', type: 'checkbox' },
      { key: 'udyamNumber', label: 'Udyam number' },
      { key: 'tdsCategory', label: 'TDS category' },
    ],
    columns: ['legalName', 'gstin', 'city', 'isMsme', 'tdsCategory'],
    importType: 'suppliers',
  },
  products: {
    title: 'Products',
    endpoint: '/m03/products',
    createEndpoint: '/m03/products',
    updateResource: '/m03/products/:id',
    updateIdField: 'productId',
    deactivateResource: '/m03/products/:id/deactivate',
    deactivateIdField: 'productId',
    fields: [
      { key: 'code', label: 'Product code', required: true },
      { key: 'name', label: 'Product name', required: true },
      { key: 'category', label: 'Category', type: 'select', options: ['RMC', 'Steel', 'Cement', 'Sand', 'Aggregate', 'Other'] },
      { key: 'hsnCode', label: 'HSN code', required: true },
      { key: 'baseUnitCode', label: 'Base unit code', required: true },
      { key: 'categoryAttributesJson', label: 'Category attributes (JSON)', type: 'json' },
      { key: 'qualityReference', label: 'Quality reference' },
      { key: 'traceable', label: 'Traceable', type: 'checkbox' },
    ],
    columns: ['code', 'name', 'category', 'hsnCode', 'baseUnitCode', 'traceable'],
    importType: 'products',
  },
  'price-lists': {
    title: 'Price lists',
    endpoint: '/m03/price-lists',
    createEndpoint: '/m03/price-lists',
    updateResource: '/m03/price-lists/:id',
    updateIdField: 'priceListId',
    deactivateResource: '/m03/price-lists/:id/deactivate',
    deactivateIdField: 'priceListId',
    fields: [
      { key: 'name', label: 'Price list name', required: true },
      { key: 'customerPartyId', label: 'Customer party ID' },
      { key: 'customerGroup', label: 'Customer group' },
      { key: 'isDefault', label: 'Default list', type: 'checkbox' },
      { key: 'validFrom', label: 'Valid from', type: 'date', required: true },
      { key: 'validTo', label: 'Valid to', type: 'date' },
    ],
    columns: ['name', 'customerPartyId', 'customerGroup', 'isDefault', 'validFrom', 'validTo'],
  },
  vehicles: {
    title: 'Vehicles and transit mixers',
    endpoint: '/m03/vehicles',
    createEndpoint: '/m03/vehicles',
    updateResource: '/m03/vehicles/:id',
    updateIdField: 'vehicleId',
    deactivateResource: '/m03/vehicles/:id/deactivate',
    deactivateIdField: 'vehicleId',
    fields: [
      { key: 'registrationNumber', label: 'Registration number', required: true },
      { key: 'type', label: 'Vehicle type', type: 'select', options: ['TransitMixer', 'Truck', 'Other'] },
      { key: 'capacity', label: 'Capacity', type: 'number', min: 0.001 },
      { key: 'capacityUnitCode', label: 'Capacity unit code', required: true },
      { key: 'ownership', label: 'Ownership', type: 'select', options: ['Own', 'Hired'] },
      { key: 'transporterPartyId', label: 'Transporter party ID' },
      { key: 'insuranceExpiresOn', label: 'Insurance expiry', type: 'date' },
      { key: 'pucExpiresOn', label: 'PUC expiry', type: 'date' },
      { key: 'fitnessExpiresOn', label: 'Fitness expiry', type: 'date' },
    ],
    columns: ['registrationNumber', 'type', 'capacity', 'ownership', 'insuranceExpiresOn', 'pucExpiresOn', 'fitnessExpiresOn'],
  },
  drivers: {
    title: 'Drivers',
    endpoint: '/m03/drivers',
    createEndpoint: '/m03/drivers',
    updateResource: '/m03/drivers/:id',
    updateIdField: 'driverId',
    deactivateResource: '/m03/drivers/:id/deactivate',
    deactivateIdField: 'driverId',
    fields: [
      { key: 'name', label: 'Driver name', required: true },
      { key: 'mobile', label: 'Mobile (+country code)', required: true },
      { key: 'licenceNumber', label: 'Licence number', required: true },
      { key: 'licenceExpiresOn', label: 'Licence expiry', type: 'date', required: true },
      { key: 'vehicleId', label: 'Assigned vehicle ID' },
    ],
    columns: ['name', 'mobile', 'licenceNumber', 'licenceExpiresOn', 'vehicleId'],
  },
  equipment: {
    title: 'Pumps and equipment',
    endpoint: '/m03/equipment',
    createEndpoint: '/m03/equipment',
    updateResource: '/m03/equipment/:id',
    updateIdField: 'equipmentId',
    deactivateResource: '/m03/equipment/:id/deactivate',
    deactivateIdField: 'equipmentId',
    fields: [
      { key: 'name', label: 'Equipment name', required: true },
      { key: 'equipmentType', label: 'Equipment type', required: true },
      { key: 'pumpType', label: 'Pump type', required: true },
      { key: 'capacity', label: 'Capacity', type: 'number' },
      { key: 'capacityUnitCode', label: 'Capacity unit code' },
      { key: 'ownerPartyId', label: 'Owner supplier', contextResource: 'supplier' },
      { key: 'insuranceExpiresOn', label: 'Insurance expiry', type: 'date' },
      { key: 'fitnessExpiresOn', label: 'Fitness expiry', type: 'date' },
    ],
    columns: ['name', 'equipmentType', 'pumpType', 'capacity', 'insuranceExpiresOn', 'fitnessExpiresOn'],
  },
  tax: {
    title: 'Tax and HSN master',
    endpoint: '/m03/tax-rates',
    createEndpoint: '/m03/tax-rates',
    deactivateResource: '/m03/tax-rates/:id/deactivate',
    deactivateIdField: 'taxRateId',
    fields: [
      { key: 'hsnCode', label: 'HSN code', required: true },
      { key: 'ratePercent', label: 'Tax rate (%)', type: 'number', min: 0 },
      { key: 'effectiveFrom', label: 'Effective from', type: 'date', required: true },
      { key: 'effectiveTo', label: 'Effective to', type: 'date' },
      { key: 'reason', label: 'Reason', required: true },
    ],
    columns: ['hsnCode', 'ratePercent', 'effectiveFrom', 'effectiveTo', 'isApproved'],
  },
  units: {
    title: 'Units',
    endpoint: '/m03/units',
    createEndpoint: '/m03/units',
    updateResource: '/m03/units/:id',
    updateIdField: 'unitCode',
    deactivateResource: '/m03/units/:id/deactivate',
    deactivateIdField: 'unitCode',
    fields: [
      { key: 'unitCode', label: 'Unit code', required: true },
      { key: 'name', label: 'Name', required: true },
      { key: 'symbol', label: 'Symbol', required: true },
    ],
    columns: ['unitCode', 'name', 'symbol', 'isActive'],
  },
  'unit-conversions': {
    title: 'Unit conversions',
    endpoint: '/m03/unit-conversions',
    createEndpoint: '/m03/unit-conversions',
    deactivateResource: '/m03/unit-conversions/:id/deactivate',
    deactivateIdField: 'conversionId',
    fields: [
      { key: 'fromUnitCode', label: 'From unit', required: true },
      { key: 'toUnitCode', label: 'To unit', required: true },
      { key: 'factor', label: 'Target units per source unit', type: 'number', min: 0.000000001 },
    ],
    columns: ['fromUnitCode', 'toUnitCode', 'toUnitsPerFromUnit', 'isActive'],
  },
  'payment-terms': {
    title: 'Payment terms',
    endpoint: '/m03/payment-terms',
    createEndpoint: '/m03/payment-terms',
    deactivateResource: '/m03/payment-terms/:id/deactivate',
    deactivateIdField: 'paymentTermId',
    fields: [
      { key: 'name', label: 'Name', required: true },
      { key: 'dueDays', label: 'Due days', type: 'number', min: 0 },
      { key: 'ledgerCode', label: 'Ledger code' },
    ],
    columns: ['name', 'dueDays', 'ledgerCode', 'isActive'],
  },
  'charge-types': {
    title: 'Charge types',
    endpoint: '/m03/charge-types',
    createEndpoint: '/m03/charge-types',
    deactivateResource: '/m03/charge-types/:id/deactivate',
    deactivateIdField: 'chargeTypeId',
    fields: [
      { key: 'code', label: 'Charge code', required: true },
      { key: 'name', label: 'Name', required: true },
      { key: 'ledgerCode', label: 'Ledger code' },
      { key: 'hsnCode', label: 'HSN code' },
      { key: 'defaultSurchargeRate', label: 'Default surcharge rate', type: 'number', min: 0 },
      { key: 'defaultTaxRatePercent', label: 'Default tax rate (%)', type: 'number', min: 0 },
    ],
    columns: ['code', 'name', 'ledgerCode', 'hsnCode', 'defaultSurchargeRate', 'defaultTaxRatePercent'],
  },
  'bank-accounts': {
    title: 'Bank and cash accounts',
    endpoint: '/m03/bank-cash-accounts',
    createEndpoint: '/m03/bank-cash-accounts',
    fields: [{ key: 'accountsJson', label: 'Account configuration (JSON array)', type: 'json' }],
    columns: ['name', 'kind', 'upi', 'accountNumber', 'ifsc'],
  },
  plants: {
    title: 'Supplier plants',
    endpoint: '/m03/plants',
    createEndpoint: '/m03/plants',
    updateResource: '/m03/plants/:id',
    updateIdField: 'plantId',
    deactivateResource: '/m03/plants/:id/deactivate',
    deactivateIdField: 'plantId',
    fields: [
      { key: 'supplierPartyId', label: 'Supplier party', required: true, contextResource: 'supplier' },
      { key: 'branchId', label: 'Branch ID' },
      { key: 'name', label: 'Plant name', required: true },
      { key: 'address', label: 'Address', required: true },
      { key: 'city', label: 'City', required: true },
      { key: 'stateCode', label: 'GST state code', required: true },
      { key: 'capacityPerHour', label: 'Capacity per hour', type: 'number' },
      { key: 'grades', label: 'Supported grades (JSON array)', type: 'json' },
    ],
    columns: ['name', 'supplierPartyId', 'city', 'capacityPerHour', 'gradesJson'],
  },
  'price-lines': {
    title: 'Price list lines',
    endpoint: '/m03/price-lists',
    createEndpoint: '/m03/price-lists',
    updateResource: '/m03/price-lists/:id/lines',
    updateIdField: 'priceListId',
    fields: [
      { key: 'priceListId', label: 'Price list ID', required: true },
      { key: 'productId', label: 'Product', required: true, contextResource: 'product' },
      { key: 'rate', label: 'Rate', type: 'number', min: 0 },
      { key: 'minimumRate', label: 'Minimum rate', type: 'number', min: 0 },
      { key: 'minimumMarginPercent', label: 'Minimum margin (%)', type: 'number', min: 0 },
    ],
    columns: ['priceListLineId', 'productId', 'rate', 'minimumRate', 'minimumMarginPercent', 'version'],
  },
  'rmc-rate-cards': {
    title: 'RMC rate cards',
    endpoint: '/m03/rmc-rate-cards',
    createEndpoint: '/m03/rmc-rate-cards',
    deactivateResource: '/m03/rmc-rate-cards/:id/deactivate',
    deactivateIdField: 'rateCardId',
    fields: [
      { key: 'productId', label: 'RMC product', required: true, contextResource: 'product' },
      { key: 'grade', label: 'Grade', required: true },
      { key: 'slumpBand', label: 'Slump band', required: true },
      { key: 'admixtureCode', label: 'Admixture code', required: true },
      { key: 'pumpType', label: 'Pump type', required: true },
      { key: 'rate', label: 'Rate', type: 'number', min: 0 },
      { key: 'admixtureChargeTypeId', label: 'Admixture charge type ID' },
      { key: 'pumpChargeTypeId', label: 'Pump charge type ID' },
      { key: 'effectiveFrom', label: 'Effective from', type: 'date', required: true },
      { key: 'effectiveTo', label: 'Effective to', type: 'date' },
    ],
    columns: ['productId', 'grade', 'slumpBand', 'admixtureCode', 'pumpType', 'rate', 'effectiveFrom', 'effectiveTo', 'isApproved'],
  },
  'category-tax-overrides': {
    title: 'Customer category GST overrides',
    endpoint: '/m03/category-tax-overrides',
    createEndpoint: '/m03/category-tax-overrides',
    fields: [
      { key: 'productId', label: 'Product', required: true, contextResource: 'product' },
      { key: 'customerCategory', label: 'Customer category', required: true },
      { key: 'ratePercent', label: 'Tax rate (%)', type: 'number', min: 0 },
      { key: 'effectiveFrom', label: 'Effective from', type: 'date', required: true },
      { key: 'effectiveTo', label: 'Effective to', type: 'date' },
      { key: 'reason', label: 'Reason', required: true },
    ],
    columns: ['productId', 'customerCategory', 'ratePercent', 'effectiveFrom', 'effectiveTo', 'isApproved'],
  },
  'customer-360': {
    title: 'Customer 360',
    endpoint: '',
    fields: [{ key: 'partyId', label: 'Customer party ID', required: true }],
    columns: [],
  },
  'supplier-bank-accounts': {
    title: 'Supplier bank accounts',
    endpoint: '/m03/parties?role=Supplier',
    fields: [],
    columns: ['legalName', 'gstin', 'city'],
  },
  'fleet-compliance': {
    title: 'Fleet compliance',
    endpoint: '/m03/fleet/expiries?days=30',
    fields: [],
    columns: [],
  },
};

const NAV = [
  ['customers', 'Customers'], ['sites', 'Sites'], ['contacts', 'Contacts'],
  ['suppliers', 'Suppliers'], ['plants', 'Plants'], ['products', 'Products'],
  ['price-lists', 'Price lists'], ['vehicles', 'Vehicles'], ['drivers', 'Drivers'],
  ['price-lines', 'Price lines'], ['rmc-rate-cards', 'RMC rate cards'],
  ['equipment', 'Pumps / equipment'], ['tax', 'Tax / HSN'],
  ['category-tax-overrides', 'GST overrides'], ['units', 'Units'],
  ['unit-conversions', 'Conversions'], ['payment-terms', 'Payment terms'],
  ['charge-types', 'Charge types'], ['bank-accounts', 'Bank / cash'],
  ['customer-360', 'Customer 360'], ['supplier-bank-accounts', 'Supplier bank accounts'],
  ['fleet-compliance', 'Fleet compliance'],
];

@Component({
  selector: 'app-m03-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, M03CreateInContextDrawerComponent],
  template: `
    <main class="page">
      <header class="heading">
        <div>
          <p class="eyebrow">P1-M03 · Masters</p>
          <h1>{{ config.title }}</h1>
          <p class="summary">Tenant-scoped construction masters. Changes are audited and referenced records are deactivated, not deleted.</p>
        </div>
        <a class="back-link" routerLink="/">Workspace</a>
      </header>

      <nav class="navigation" aria-label="Master screens">
        @for (item of navigation; track item[0]) {
          <a [routerLink]="'/m03/' + item[0]" [class.active]="screen === item[0]">{{ item[1] }}</a>
        }
      </nav>

      @if (error) { <p class="notice error" role="alert">{{ error }}</p> }
      @if (success) { <p class="notice success" role="status">{{ success }}</p> }
      @if (loading) { <p class="notice" role="status">Loading {{ config.title.toLowerCase() }}…</p> }

      @if (screen === 'fleet-compliance') {
        <section class="panel">
          <div class="section-heading">
            <h2>Expiry warnings and eligibility</h2>
            <div class="actions">
              <button type="button" class="secondary" (click)="load()">Refresh expiry indicators</button>
              @if (bankCapabilities?.canEdit) {
                <button type="button" (click)="refreshFleetNotifications()" [disabled]="saving">
                  {{ saving ? 'Publishing…' : 'Create internal notifications' }}
                </button>
              }
            </div>
          </div>
          <p>Expiry alerts use the existing M02 in-app notification store. External delivery is not configured or verified.</p>
          @if (rows.length) {
            <div class="table-scroll">
              <table><thead><tr><th>Asset</th><th>Document</th><th>Expires</th><th>Days</th><th>Urgency</th><th>Eligibility</th></tr></thead>
                <tbody>@for (alert of rows; track alert['id'] + ':' + alert['document']) {
                  <tr>
                    <td>{{ alert['type'] }} · {{ alert['name'] }}</td><td>{{ alert['document'] }}</td>
                    <td>{{ alert['expiresOn'] }}</td><td>{{ alert['daysRemaining'] }}</td>
                    <td class="warning">{{ alert['urgency'] }}</td>
                    <td><button type="button" class="secondary row-action" (click)="checkFleetEligibility(alert)">Check</button></td>
                  </tr>
                }</tbody>
              </table>
            </div>
          } @else if (!loading) {
            <p>No fleet documents are due within the selected 30-day horizon.</p>
          }
          @if (fleetEligibility) {
            <p class="notice" [class.error]="fleetEligibility['eligible'] === false">
              Eligibility: {{ fleetEligibility['eligible'] ? 'Eligible' : 'Blocked' }}.
              {{ fleetEligibility['message'] }}
            </p>
          }
        </section>
      } @else if (screen === 'supplier-bank-accounts') {
        <section class="panel">
          <h2>Select supplier</h2>
          <div class="form-grid">
            <label>Supplier
              <select name="selectedSupplierId" [(ngModel)]="selectedSupplierId" (ngModelChange)="onSupplierChanged()">
                <option value="">Select supplier…</option>
                @for (supplier of rows; track supplier['partyId']) {
                  <option [value]="supplier['partyId']">{{ supplier['legalName'] }} · {{ supplier['partyId'] }}</option>
                }
              </select>
            </label>
            <div class="actions">
              <button type="button" (click)="loadSupplierAccounts()" [disabled]="!selectedSupplierId || loading">
                Load accounts
              </button>
              @if (bankCapabilities?.canCreate || bankCapabilities?.canEdit) {
                <button type="button" class="secondary" (click)="startSupplierBankAccount()">
                  Add bank account
                </button>
              }
            </div>
          </div>
        </section>
        @if (bankFormOpen) {
          <section class="panel">
            <h2>{{ editingBankAccount ? 'Edit supplier bank account' : 'Add supplier bank account' }}</h2>
            <p>Account numbers are never returned. Leave the replacement field blank to keep the current number.</p>
            <form class="form-grid" (ngSubmit)="saveSupplierBankAccount()">
              <label>Account holder<input name="accountHolder" [(ngModel)]="bankForm.accountHolder" required /></label>
              <label>IFSC<input name="ifsc" [(ngModel)]="bankForm.ifsc" required /></label>
              <label>Bank name<input name="bankName" [(ngModel)]="bankForm.bankName" /></label>
              <label>{{ editingBankAccount ? 'Replace account number (optional)' : 'Account number' }}
                <input name="accountNumber" [(ngModel)]="bankForm.accountNumber"
                  [required]="!editingBankAccount" autocomplete="off" />
              </label>
              <label class="check-field"><input type="checkbox" name="isPrimary" [(ngModel)]="bankForm.isPrimary" />Primary account</label>
              <div class="actions wide">
                <button type="submit" [disabled]="saving">{{ saving ? 'Saving…' : 'Save account' }}</button>
                <button type="button" class="secondary" (click)="cancelSupplierBankEdit()">Cancel</button>
              </div>
            </form>
          </section>
        }
        @if (supplierBankAccounts.length) {
          <section class="panel table-panel">
            <h2>Active accounts · masked values only</h2>
            <div class="table-scroll">
              <table><thead><tr><th>Holder</th><th>Account number</th><th>IFSC</th><th>Bank</th><th>Primary</th><th>Actions</th></tr></thead>
                <tbody>@for (account of supplierBankAccounts; track account['bankAccountId']) {
                  <tr>
                    <td>{{ account['accountHolder'] }}</td><td>{{ account['maskedAccountNumber'] }}</td>
                    <td>{{ account['ifsc'] }}</td><td>{{ account['bankName'] || '—' }}</td>
                    <td>{{ account['isPrimary'] ? 'Yes' : 'No' }}</td>
                    <td class="row-actions">
                      @if (bankCapabilities?.canEdit) {
                        <button type="button" class="secondary row-action" (click)="editSupplierBankAccount(account)">Edit</button>
                      }
                      @if (bankCapabilities?.canDeactivate) {
                        <button type="button" class="secondary row-action" (click)="deactivateSupplierBankAccount(account)">Deactivate</button>
                      }
                    </td>
                  </tr>
                }</tbody>
              </table>
            </div>
          </section>
        } @else if (selectedSupplierId && !loading) {
          <section class="panel empty"><h2>No active bank accounts</h2><p>Add an account if your permissions allow it.</p></section>
        }
        @if (bankCapabilities?.canViewAudit) {
          <section class="panel">
            <div class="section-heading"><h2>Supplier bank-account audit history</h2>
              <button type="button" class="secondary" (click)="loadSupplierBankAudit()">Refresh audit history</button>
            </div>
            @for (event of bankAudit; track event['eventId']) {
              <p>{{ event['at'] }} · {{ event['actorSubject'] }} · {{ event['action'] }} · {{ event['reason'] || 'No reason recorded' }}</p>
            }
          </section>
        } @else {
          <p class="notice">Audit history requires the server-enforced audit.view permission.</p>
        }
      } @else if (screen === 'customer-360') {
        <section class="panel">
          <h2>Customer 360</h2>
          <form (ngSubmit)="loadCustomer360()" class="form-grid">
            <label>Customer
              <select name="partyId" [(ngModel)]="form['partyId']" required>
                <option value="">Select customer…</option>
                @for (customer of customerChoices; track customer['partyId']) {
                  <option [value]="customer['partyId']">{{ customer['legalName'] }} · {{ customer['partyId'] }}</option>
                }
              </select>
            </label>
            <button type="submit" [disabled]="loading || !form['partyId']">
              {{ loading ? 'Loading…' : 'Open customer view' }}
            </button>
          </form>
          @if (customer360) {
            <section class="detail-grid" aria-label="Customer profile">
              <article class="detail-card">
                <h3>Profile and GST</h3>
                <p><strong>{{ customer360.party['legalName'] }}</strong> {{ customer360.party['tradeName'] }}</p>
                <p>GSTIN: {{ customer360.party['gstin'] || 'Not provided' }} · PAN: {{ customer360.party['pan'] || 'Not provided' }}</p>
                <p>{{ customer360.party['billingAddress'] || 'No billing address' }} · {{ customer360.party['city'] || 'No city' }}, state {{ customer360.party['stateCode'] || '—' }}</p>
              </article>
              <article class="detail-card">
                <h3>Credit settings and status</h3>
                <p>Limit: {{ customer360.party['creditLimit'] }} · Credit days: {{ customer360.party['creditDays'] }}</p>
                <p>Policy: {{ customer360.party['creditPolicy'] }} · Hold status: {{ customer360.party['creditStatus'] }}</p>
                <p>Current evaluation: <strong>{{ customer360.creditDecision.status }}</strong></p>
                @for (reason of customer360.creditDecision.reasons; track reason) { <p class="warning">{{ reason }}</p> }
              </article>
            </section>
            <section class="detail-grid">
              <article class="detail-card">
                <h3>Customer sites ({{ customer360.sites.length }})</h3>
                @if (customer360.sites.length) {
                  @for (site of customer360.sites; track site['siteId']) {
                    <p><strong>{{ site['name'] }}</strong> · {{ site['address'] }}, {{ site['city'] }} · GSTIN {{ site['gstin'] || '—' }}</p>
                  }
                } @else { <p>No customer sites are recorded.</p> }
              </article>
              <article class="detail-card">
                <h3>Contacts ({{ customer360.contacts.length }})</h3>
                @if (customer360.contacts.length) {
                  @for (contact of customer360.contacts; track contact['contactId']) {
                    <p><strong>{{ contact['name'] }}</strong> · {{ contact['role'] }} · {{ contact['email'] || 'No email' }} · {{ contact['mobile'] || 'No mobile' }}</p>
                  }
                } @else { <p>No contacts are recorded.</p> }
              </article>
            </section>
            <section class="detail-card">
              <h3>Current pricing agreements</h3>
              @if (customer360.pricingAgreements.length) {
                @for (agreement of customer360.pricingAgreements; track agreement['priceListId']) {
                  <p>{{ agreement['name'] }} · {{ agreement['validFrom'] }} to {{ agreement['validTo'] || 'open ended' }}</p>
                }
              } @else { <p>No active applicable price agreement is recorded.</p> }
            </section>
            <section class="detail-card">
              <h3>Credit exposure</h3>
              @if (customer360.creditFacts.available) {
                <p>Outstanding {{ customer360.creditFacts.outstanding }} · Overdue {{ customer360.creditFacts.overdue }}</p>
              } @else {
                <p class="warning">{{ customer360.creditFacts.unavailableReason }} No financial values are estimated or fabricated.</p>
              }
            </section>
            <section class="detail-card">
              <h3>Sales and collection metrics</h3>
              <p class="warning">{{ customer360.metrics.unavailableReason }}</p>
            </section>
            <section class="detail-card">
              <h3>Activity and audit history</h3>
              @if (!customer360.activityAvailable) {
                <p>Audit history requires the server-enforced audit.view permission.</p>
              } @else if (customer360.activity.length) {
                @for (event of customer360.activity; track event['eventId']) {
                  <p>{{ event['at'] }} · {{ event['actorSubject'] }} · {{ event['action'] }} · {{ event['entityType'] }}</p>
                }
              } @else { <p>No party, site or contact audit events are available.</p> }
            </section>
            <section class="detail-card">
              <h3>Linked documents</h3>
              <p>{{ customer360.linkedDocuments.message }}</p>
              <p>Future integrations: {{ customer360.unavailableIntegrations.join(', ') }}</p>
            </section>
          }
        </section>
      } @else {
        @if (config.createEndpoint) {
          <section class="panel">
            <div class="section-heading">
              <h2>Add {{ config.title | lowercase | slice:0:-1 }}</h2>
              <button type="button" class="secondary" (click)="formOpen = !formOpen">
                {{ formOpen ? 'Close form' : 'New record' }}
              </button>
            </div>
            @if (formOpen) {
              <form (ngSubmit)="save()" class="form-grid">
                @for (field of config.fields; track field.key) {
                  @if (field.type === 'checkbox') {
                    <label class="check-field">
                      <input type="checkbox" [name]="field.key" [(ngModel)]="form[field.key]" />
                      {{ field.label }}
                    </label>
                  } @else if (field.type === 'json') {
                    <label class="wide">{{ field.label }}
                      <textarea [name]="field.key" [(ngModel)]="form[field.key]" rows="4"></textarea>
                    </label>
                  } @else if (field.type === 'select') {
                    <label>{{ field.label }}
                      <select [name]="field.key" [(ngModel)]="form[field.key]" [required]="!!field.required">
                        <option value="">Select…</option>
                        @for (option of field.options ?? []; track option) {
                          <option [value]="option">{{ option }}</option>
                        }
                      </select>
                    </label>
                  } @else if (contextOptions(field.key).length) {
                    <label>{{ field.label }}
                      <select [name]="field.key" [(ngModel)]="form[field.key]" [required]="!!field.required">
                        <option value="">Select…</option>
                        @for (option of contextOptions(field.key); track option.id) {
                          <option [value]="option.id">{{ option.label }}</option>
                        }
                      </select>
                      <button type="button" class="secondary context-button" (click)="openContextCreate(field)">
                        Create {{ field.contextResource }}
                      </button>
                    </label>
                  } @else {
                    <label>{{ field.label }}
                      <input [type]="field.type ?? 'text'" [name]="field.key"
                        [(ngModel)]="form[field.key]" [required]="!!field.required" [min]="field.min ?? null" />
                    </label>
                  }
                }
                <div class="actions wide">
                  <button type="submit" [disabled]="saving">{{ saving ? 'Saving…' : (editingRow ? 'Save changes' : 'Save master') }}</button>
                  @if (editingRow) { <button type="button" class="secondary" (click)="cancelEdit()">Cancel edit</button> }
                  @if (screen === 'price-lists') {
                    <a class="text-link" routerLink="/m03/price-lines">Manage price lines</a>
                    <a class="text-link" routerLink="/m03/rmc-rate-cards">Manage RMC rate cards</a>
                  }
                  @if (screen === 'suppliers') {
                    <a class="text-link" routerLink="/m03/plants">Manage supplier plants</a>
                  }
                  @if (screen === 'price-lines') {
                    <button type="button" class="secondary" (click)="loadPriceLines()">Load list rates</button>
                  }
                </div>
              </form>
            }
          </section>
        }

        @if (screen === 'tax') {
          <section class="panel">
            <h2>Resolve effective tax inputs</h2>
            <p>Resolves an approved effective rate and validates GSTIN/place-of-supply identity. This does not calculate or post tax.</p>
            <form class="form-grid" (ngSubmit)="resolveTaxRate()">
              <label>Product
                <select name="taxProductId" [(ngModel)]="taxLookup.productId" required>
                  <option value="">Select product…</option>
                  @for (product of taxProducts; track product['productId']) {
                    <option [value]="product['productId']">{{ product['code'] }} · {{ product['name'] }} · HSN {{ product['hsnCode'] }}</option>
                  }
                </select>
              </label>
              <label>Customer category<input name="customerCategory" [(ngModel)]="taxLookup.customerCategory" /></label>
              <label>Seller GSTIN<input name="sellerGstin" [(ngModel)]="taxLookup.sellerGstin" /></label>
              <label>Buyer GSTIN<input name="buyerGstin" [(ngModel)]="taxLookup.buyerGstin" /></label>
              <label>Place-of-supply state code<input name="placeOfSupplyStateCode" [(ngModel)]="taxLookup.placeOfSupplyStateCode" /></label>
              <label>Effective on<input type="date" name="asOf" [(ngModel)]="taxLookup.asOf" /></label>
              <button type="submit" [disabled]="saving || !taxLookup.productId">Resolve approved rate</button>
            </form>
            @if (taxResolution) {
              <p role="status">Approved HSN rate: {{ taxResolution['ratePercent'] }}% · {{ taxResolution['source'] }} · effective {{ taxResolution['effectiveFrom'] }} to {{ taxResolution['effectiveTo'] || 'open ended' }}</p>
              <p>Seller GSTIN {{ taxResolution['sellerGstin'] || 'not supplied' }} · Buyer GSTIN {{ taxResolution['buyerGstin'] || 'not supplied' }} · Place of supply {{ taxResolution['placeOfSupplyStateCode'] || 'not supplied' }}</p>
            }
          </section>
        }

        @if (config.importType) {
          <section class="panel import-panel">
            <h2>Excel / CSV import</h2>
            <p>Preview validates rows and reports duplicate warnings before any master data is changed.</p>
            <div class="import-controls">
              <button type="button" class="secondary" (click)="downloadTemplate()">Download CSV template</button>
              <input type="file" accept=".xlsx,.csv" (change)="selectImport($event)" aria-label="Choose an Excel or CSV file" />
              <button type="button" class="secondary" (click)="previewImport()" [disabled]="!importFile || saving">
                Preview import
              </button>
            </div>
            @if (importPreview) {
              <p>{{ importPreview.validRows }} valid rows · {{ importPreview.errorRows.length }} row errors</p>
              @if (importPreview.duplicateWarnings.length) {
                <p class="warning">{{ importPreview.duplicateWarnings.length }} possible duplicate rows were found. Review before confirming.</p>
              }
              <label>Duplicate handling
                <select [(ngModel)]="duplicateHandling" name="duplicateHandling">
                  <option value="skip">Skip duplicates</option>
                  <option value="update">Update existing master</option>
                </select>
              </label>
              <button type="button" (click)="confirmImport()" [disabled]="importPreview.validRows === 0">Confirm valid rows</button>
              @if (importPreview.errorRows.length) {
                <ul class="errors">
                  @for (rowError of importPreview.errorRows; track rowError.rowNumber) {
                    <li>Row {{ rowError.rowNumber }}: {{ rowError.message }}</li>
                  }
                </ul>
              }
            }
            @if (confirmedBatchId) {
              <button type="button" class="secondary" (click)="undoImport()">Undo last batch (24 hours)</button>
            }
          </section>
        }
      }

      @if (contextResource) {
        <app-m03-create-in-context-drawer
          [resource]="contextResource"
          [open]="true"
          [context]="contextValues"
          (created)="onContextCreated($event)"
          (closed)="closeContextCreate()" />
      }

      @if (config.columns.length && rows.length) {
        <section class="panel table-panel">
          <div class="section-heading">
            <h2>{{ config.title }}</h2>
            <button type="button" class="secondary" (click)="load()">Refresh</button>
          </div>
          <label class="search-field">Filter loaded records
            <input name="searchText" [(ngModel)]="searchText" placeholder="Search visible fields" />
          </label>
          @if (!filteredRows().length) {
            <p class="empty">No loaded records match this search.</p>
          } @else {
          <div class="table-scroll">
            <table>
              <thead><tr>
                @for (column of config.columns; track column) { <th scope="col">{{ humanize(column) }}</th> }
                @if (config.deactivateResource || config.updateResource) { <th scope="col">Actions</th> }
              </tr></thead>
              <tbody>
                @for (row of filteredRows(); track row['partyId'] ?? row['productId'] ?? row['siteId'] ?? row['id'] ?? $index) {
                  <tr>
                    @for (column of config.columns; track column) { <td>{{ cell(row[column]) }}</td> }
                    @if (config.deactivateResource || config.updateResource) {
                      <td class="row-actions">
                        @if (config.updateResource) { <button type="button" class="secondary row-action" (click)="edit(row)">Edit</button> }
                        @if (config.deactivateResource) { <button type="button" class="secondary row-action" (click)="deactivate(row)">Deactivate</button> }
                      </td>
                    }
                  </tr>
                }
              </tbody>
            </table>
          </div>
          }
        </section>
      } @else if (!loading && screen !== 'customer-360') {
        <section class="panel empty"><h2>No records yet</h2><p>Records visible to your current tenant and permissions will appear here.</p></section>
      }
    </main>
  `,
  styles: `
    .page { margin: 0 auto; max-width: 82rem; padding: clamp(1rem, 4vw, 3rem); }
    .heading, .section-heading { align-items: center; display: flex; gap: 1rem; justify-content: space-between; }
    .heading { margin-bottom: 1.5rem; }
    h1 { font-size: clamp(1.8rem, 4vw, 2.7rem); letter-spacing: -.035em; margin: 0; }
    h2 { font-size: 1.2rem; margin: 0; }
    .eyebrow { color: var(--accent); font-size: .75rem; font-weight: 750; letter-spacing: .1em; margin: 0 0 .5rem; text-transform: uppercase; }
    .summary, .panel p { color: var(--muted); line-height: 1.55; }
    .back-link, .text-link { color: var(--accent); font-weight: 650; }
    .navigation { display: flex; gap: .5rem; margin: 1.25rem 0 1.5rem; overflow-x: auto; padding-bottom: .5rem; }
    .navigation a { border: 1px solid var(--border); border-radius: 999px; color: var(--text); flex: 0 0 auto; padding: .55rem .8rem; text-decoration: none; }
    .navigation a.active { background: var(--accent); border-color: var(--accent); color: white; }
    .panel { background: var(--surface, white); border: 1px solid var(--border); border-radius: 1rem; margin: 1rem 0; padding: clamp(1rem, 2.5vw, 1.5rem); }
    .section-heading { margin-bottom: 1rem; }
    .form-grid { display: grid; gap: 1rem; grid-template-columns: repeat(auto-fit, minmax(min(100%, 15rem), 1fr)); }
    label { display: grid; font-size: .9rem; font-weight: 650; gap: .4rem; }
    input, select, textarea { background: var(--surface, white); border: 1px solid var(--border); border-radius: .55rem; color: var(--text); font: inherit; min-height: 2.7rem; padding: .55rem .7rem; width: 100%; }
    input[type=checkbox] { min-height: auto; width: 1.1rem; }
    .check-field { align-items: center; display: flex; gap: .6rem; }
    .wide { grid-column: 1 / -1; }
    .actions, .import-controls { align-items: center; display: flex; flex-wrap: wrap; gap: .75rem; }
    button { background: var(--accent); border: 1px solid var(--accent); border-radius: .55rem; color: white; cursor: pointer; font: inherit; font-weight: 650; min-height: 2.7rem; padding: .55rem 1rem; }
    button:disabled { cursor: wait; opacity: .65; }
    button.secondary { background: transparent; color: var(--accent); }
    .row-action { min-height: 2rem; padding: .35rem .65rem; }
    .context-button { justify-self: start; min-height: 2rem; padding: .3rem .55rem; }
    .search-field { margin: 0 0 1rem; max-width: 28rem; }
    .notice { border-radius: .6rem; padding: .85rem 1rem; }
    .error { background: #fff1ed; color: #8a2d19; }
    .success { background: #eef8f1; color: #245d36; }
    .table-scroll { overflow-x: auto; }
    table { border-collapse: collapse; min-width: 40rem; width: 100%; }
    th, td { border-bottom: 1px solid var(--border); padding: .75rem .6rem; text-align: left; }
    th { color: var(--muted); font-size: .78rem; text-transform: uppercase; }
    .empty { color: var(--muted); }
    .snapshot { background: var(--surface, white); max-height: 30rem; overflow: auto; white-space: pre-wrap; }
    .errors { color: #8a2d19; max-height: 14rem; overflow: auto; }
    @media (max-width: 40rem) { .heading { align-items: start; flex-direction: column; } }
  `,
})
export class M03Page implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(ApiClient);
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  readonly navigation = NAV;
  screen = 'customers';
  config = CONFIGS['customers'];
  rows: Record<string, unknown>[] = [];
  private lookups: Record<string, { id: string; label: string }[]> = {};
  form: Record<string, unknown> = {};
  editingRow: Record<string, unknown> | null = null;
  contextResource: M03ContextResource | null = null;
  contextValues: Record<string, unknown> = {};
  private contextTargetKey = '';
  loading = false;
  saving = false;
  searchText = '';
  bankVersion = 0;
  selectedSupplierId = '';
  supplierBankAccounts: Record<string, unknown>[] = [];
  bankAudit: Record<string, unknown>[] = [];
  bankCapabilities: {
    canCreate: boolean;
    canEdit: boolean;
    canDeactivate: boolean;
    canViewAudit: boolean;
  } | null = null;
  bankFormOpen = false;
  editingBankAccount: Record<string, unknown> | null = null;
  bankForm = { accountHolder: '', accountNumber: '', ifsc: '', bankName: '', isPrimary: false };
  fleetEligibility: Record<string, unknown> | null = null;
  taxProducts: Record<string, unknown>[] = [];
  taxResolution: Record<string, unknown> | null = null;
  taxLookup = {
    productId: '',
    customerCategory: '',
    sellerGstin: '',
    buyerGstin: '',
    placeOfSupplyStateCode: '',
    asOf: '',
  };
  formOpen = true;
  error = '';
  success = '';
  customer360: Customer360Response | null = null;
  customerChoices: Record<string, unknown>[] = [];
  importFile: File | null = null;
  importPreview: {
    batchId: string;
    validRows: number;
    errorRows: { rowNumber: number; message: string }[];
    duplicateWarnings: unknown[];
  } | null = null;
  confirmedBatchId: string | null = null;
  duplicateHandling = 'skip';

  ngOnInit(): void {
    this.route.paramMap.subscribe((params) => {
      this.screen = params.get('screen') ?? 'customers';
      this.config = CONFIGS[this.screen] ?? CONFIGS['customers'];
      this.rows = [];
      this.searchText = '';
      this.lookups = {};
      this.customer360 = null;
      this.customerChoices = [];
      this.form = {};
      this.editingRow = null;
      this.selectedSupplierId = '';
      this.supplierBankAccounts = [];
      this.bankAudit = [];
      this.bankCapabilities = null;
      this.bankFormOpen = false;
      this.editingBankAccount = null;
      this.fleetEligibility = null;
      this.taxProducts = [];
      this.taxResolution = null;
      this.taxLookup = {
        productId: '', customerCategory: '', sellerGstin: '',
        buyerGstin: '', placeOfSupplyStateCode: '', asOf: '',
      };
      this.formOpen = this.screen !== 'customer-360';
      if (this.screen !== 'customer-360') {
        this.load();
        this.loadContextLookups();
      }
      if (this.screen === 'customer-360') this.loadCustomerChoices();
      if (this.screen === 'supplier-bank-accounts' || this.screen === 'fleet-compliance') this.loadBankCapabilities();
      if (this.screen === 'tax') this.loadTaxProducts();
    });
  }

  private loadCustomerChoices(): void {
    this.api.get<Record<string, unknown>[]>('/m03/parties?role=Customer').pipe(
      catchError((error: { error?: { detail?: string; title?: string } }) => {
        this.error = error.error?.detail ?? error.error?.title ?? 'Unable to load customers.';
        return of([]);
      }),
    ).subscribe((rows) => this.customerChoices = Array.isArray(rows) ? rows : []);
  }

  private loadTaxProducts(): void {
    this.api.get<Record<string, unknown>[]>('/m03/products').pipe(
      catchError((error: { error?: { detail?: string; title?: string } }) => {
        this.error = error.error?.detail ?? error.error?.title ?? 'Unable to load products for tax resolution.';
        return of([]);
      }),
    ).subscribe((rows) => this.taxProducts = Array.isArray(rows) ? rows : []);
  }

  resolveTaxRate(): void {
    if (!this.taxLookup.productId || this.saving) return;
    this.saving = true;
    this.error = '';
    this.taxResolution = null;
    this.api.post<Record<string, unknown>, unknown>('/m03/tax-rates/resolve', {
      productId: this.taxLookup.productId,
      customerCategory: this.taxLookup.customerCategory || null,
      asOf: this.taxLookup.asOf || null,
      sellerGstin: this.taxLookup.sellerGstin || null,
      buyerGstin: this.taxLookup.buyerGstin || null,
      placeOfSupplyStateCode: this.taxLookup.placeOfSupplyStateCode || null,
    }).pipe(catchError((error: { error?: { detail?: string; title?: string } }) => {
      this.error = error.error?.detail ?? error.error?.title ?? 'Unable to resolve the effective tax rate.';
      this.saving = false;
      return of(null);
    })).subscribe((result) => {
      this.taxResolution = result as Record<string, unknown> | null;
      this.saving = false;
    });
  }

  loadBankCapabilities(): void {
    this.api.get<{
      canCreate: boolean;
      canEdit: boolean;
      canDeactivate: boolean;
      canViewAudit: boolean;
    }>('/m03/capabilities').pipe(
      catchError((error: { error?: { detail?: string; title?: string } }) => {
        this.error = error.error?.detail ?? error.error?.title ?? 'Unable to load bank-account permissions.';
        return of(null);
      }),
    ).subscribe((capabilities) => this.bankCapabilities = capabilities);
  }

  refreshFleetNotifications(): void {
    this.saving = true;
    this.error = '';
    this.api.post<{ created: number; alerts: number; deliveryStatus: string }, Record<string, never>>(
      '/m03/fleet/expiry-notifications/refresh?days=30', {},
    ).pipe(catchError((error: { error?: { detail?: string; title?: string } }) => {
      this.error = error.error?.detail ?? error.error?.title ?? 'Unable to create internal fleet notifications.';
      this.saving = false;
      return of(null);
    })).subscribe((result) => {
      if (result) this.success = `${result.created} internal notification(s) created from ${result.alerts} expiry alert(s). ${result.deliveryStatus}`;
      this.saving = false;
    });
  }

  checkFleetEligibility(alert: Record<string, unknown>): void {
    const type = String(alert['type'] ?? '');
    const id = String(alert['id'] ?? '');
    const route = type === 'Vehicle' ? 'vehicles' : type === 'Driver' ? 'drivers' : 'equipment';
    const path = type === 'Vehicle'
      ? `/m03/fleet/vehicles/${encodeURIComponent(id)}/dispatch-eligibility`
      : `/m03/fleet/${route}/${encodeURIComponent(id)}/eligibility`;
    this.fleetEligibility = null;
    this.api.get<Record<string, unknown>>(path).pipe(
      catchError((error: { error?: { detail?: string; title?: string } }) => {
        this.error = error.error?.detail ?? error.error?.title ?? 'Unable to check asset eligibility.';
        return of(null);
      }),
    ).subscribe((result) => {
      if (!result) return;
      const expired = (result['expiredDocuments'] as string[] | undefined) ?? [];
      this.fleetEligibility = {
        ...result,
        message: expired.length ? `Expired documents: ${expired.join(', ')}.` : 'No expired documents were found.',
      };
    });
  }

  onSupplierChanged(): void {
    this.supplierBankAccounts = [];
    this.bankAudit = [];
    this.cancelSupplierBankEdit();
  }

  loadSupplierAccounts(): void {
    if (!this.selectedSupplierId) return;
    this.loading = true;
    this.error = '';
    this.api.get<Record<string, unknown>[]>(
      `/m03/parties/${encodeURIComponent(this.selectedSupplierId)}/bank-accounts`,
    ).pipe(catchError((error: { error?: { detail?: string; title?: string } }) => {
      this.error = error.error?.detail ?? error.error?.title ?? 'Unable to load supplier bank accounts.';
      this.loading = false;
      return of([]);
    })).subscribe((accounts) => {
      this.supplierBankAccounts = Array.isArray(accounts) ? accounts : [];
      this.loading = false;
      if (this.bankCapabilities?.canViewAudit) this.loadSupplierBankAudit();
    });
  }

  startSupplierBankAccount(): void {
    if (!this.selectedSupplierId || !this.bankCapabilities?.canCreate) {
      this.error = 'Select a supplier and confirm your create permission.';
      return;
    }
    this.editingBankAccount = null;
    this.bankForm = { accountHolder: '', accountNumber: '', ifsc: '', bankName: '', isPrimary: false };
    this.bankFormOpen = true;
    this.error = '';
  }

  editSupplierBankAccount(account: Record<string, unknown>): void {
    if (!this.bankCapabilities?.canEdit) {
      this.error = 'You do not have permission to edit supplier bank accounts.';
      return;
    }
    this.editingBankAccount = account;
    this.bankForm = {
      accountHolder: String(account['accountHolder'] ?? ''),
      accountNumber: '',
      ifsc: String(account['ifsc'] ?? ''),
      bankName: String(account['bankName'] ?? ''),
      isPrimary: Boolean(account['isPrimary']),
    };
    this.bankFormOpen = true;
    this.error = '';
  }

  cancelSupplierBankEdit(): void {
    this.bankFormOpen = false;
    this.editingBankAccount = null;
    this.bankForm = { accountHolder: '', accountNumber: '', ifsc: '', bankName: '', isPrimary: false };
  }

  saveSupplierBankAccount(): void {
    if (!this.selectedSupplierId || this.saving) return;
    if (!this.bankForm.accountHolder.trim() || !/^[A-Z]{4}0[A-Z0-9]{6}$/i.test(this.bankForm.ifsc.trim())) {
      this.error = 'Enter an account holder and valid IFSC.';
      return;
    }
    if (!this.editingBankAccount && !this.bankForm.accountNumber.trim()) {
      this.error = 'Account number is required when adding an account.';
      return;
    }
    if (/^[•*]+/.test(this.bankForm.accountNumber.trim())) {
      this.error = 'Masked account values cannot be used as a replacement. Enter a new account number or leave blank.';
      return;
    }

    this.saving = true;
    this.error = '';
    const body = {
      accountHolder: this.bankForm.accountHolder.trim(),
      accountNumber: this.bankForm.accountNumber.trim() || null,
      ifsc: this.bankForm.ifsc.trim().toUpperCase(),
      bankName: this.bankForm.bankName.trim() || null,
      isPrimary: this.bankForm.isPrimary,
    };
    const request = this.editingBankAccount
      ? this.api.put<unknown, unknown>(
        `/m03/party-bank-accounts/${encodeURIComponent(String(this.editingBankAccount['bankAccountId']))}`,
        { ...body, version: this.editingBankAccount['version'] },
      )
      : this.api.post<unknown, unknown>(
        '/m03/party-bank-accounts',
        { ...body, partyId: this.selectedSupplierId, accountNumber: this.bankForm.accountNumber.trim() },
      );
    let failed = false;
    request.pipe(catchError((error: { error?: { detail?: string; title?: string } }) => {
      failed = true;
      this.error = error.error?.detail ?? error.error?.title ?? 'Unable to save the supplier bank account.';
      this.saving = false;
      return of(null);
    })).subscribe((result) => {
      if (!failed && result !== null) {
        this.success = 'Supplier bank account saved. The API returned only a masked number.';
        this.cancelSupplierBankEdit();
        this.loadSupplierAccounts();
      }
      this.saving = false;
    });
  }

  deactivateSupplierBankAccount(account: Record<string, unknown>): void {
    if (!this.bankCapabilities?.canDeactivate ||
        !window.confirm('Deactivate this supplier bank account?')) return;
    const id = String(account['bankAccountId'] ?? '');
    const version = String(account['version'] ?? '');
    if (!id || !version) return;
    this.saving = true;
    this.error = '';
    this.api.post<unknown, Record<string, never>>(
      `/m03/party-bank-accounts/${encodeURIComponent(id)}/deactivate?version=${encodeURIComponent(version)}`, {},
    ).pipe(catchError((error: { error?: { detail?: string; title?: string } }) => {
      this.error = error.error?.detail ?? error.error?.title ?? 'Unable to deactivate the supplier bank account.';
      this.saving = false;
      return of(null);
    })).subscribe((result) => {
      if (result !== null) {
        this.success = 'Supplier bank account deactivated.';
        this.loadSupplierAccounts();
      }
      this.saving = false;
    });
  }

  loadSupplierBankAudit(): void {
    if (!this.selectedSupplierId) return;
    this.api.get<Record<string, unknown>[]>(
      `/m03/parties/${encodeURIComponent(this.selectedSupplierId)}/bank-accounts/audit`,
    ).pipe(catchError((error: { error?: { detail?: string; title?: string } }) => {
      this.error = error.error?.detail ?? error.error?.title ?? 'Unable to load supplier bank-account audit history.';
      return of([]);
    })).subscribe((events) => this.bankAudit = Array.isArray(events) ? events : []);
  }

  contextOptions(fieldKey: string): { id: string; label: string }[] {
    return this.lookups[fieldKey] ?? [];
  }

  openContextCreate(field: MasterField): void {
    if (!field.contextResource) return;
    if (field.contextResource === 'site' && !this.form['partyId']) {
      this.error = 'Select a customer before creating a customer site.';
      return;
    }
    this.contextTargetKey = field.key;
    this.contextResource = field.contextResource;
    this.contextValues = field.contextResource === 'site'
      ? { customerPartyId: this.form['partyId'] }
      : {};
  }

  closeContextCreate(): void {
    this.contextResource = null;
    this.contextValues = {};
    this.contextTargetKey = '';
  }

  onContextCreated(record: Record<string, unknown>): void {
    const resource = this.contextResource ?? 'customer';
    const idField: Record<M03ContextResource, string> = {
      customer: 'partyId',
      site: 'siteId',
      contact: 'contactId',
      supplier: 'partyId',
      product: 'productId',
    };
    const id = record[idField[resource]];
    if (id === undefined || id === null) {
      this.error = 'The API saved the record but did not return its selection ID.';
      return;
    }
    this.form[this.contextTargetKey] = id;
    this.success = 'Created record selected. Your existing form values were retained.';
    this.closeContextCreate();
    this.loadContextLookups();
  }

  private loadContextLookups(): void {
    const requests: {
      key: string;
      endpoint: string;
      label: (row: Record<string, unknown>) => string;
      id: string;
    }[] = [];
    if (this.screen === 'sites') {
      requests.push({
        key: 'customerPartyId', endpoint: '/m03/parties?role=Customer',
        label: row => String(row['legalName'] ?? ''), id: 'partyId',
      });
    } else if (this.screen === 'contacts') {
      requests.push({
        key: 'partyId', endpoint: '/m03/parties',
        label: row => String(row['legalName'] ?? ''), id: 'partyId',
      });
      requests.push({
        key: 'siteId', endpoint: '/m03/sites',
        label: row => `${row['name'] ?? ''} · ${row['city'] ?? ''}`, id: 'siteId',
      });
    } else if (this.screen === 'plants') {
      requests.push({
        key: 'supplierPartyId', endpoint: '/m03/parties?role=Supplier',
        label: row => String(row['legalName'] ?? ''), id: 'partyId',
      });
    } else if (['price-lines', 'rmc-rate-cards', 'category-tax-overrides'].includes(this.screen)) {
      requests.push({
        key: 'productId', endpoint: '/m03/products',
        label: row => `${row['code'] ?? ''} · ${row['name'] ?? ''}`, id: 'productId',
      });
    } else if (this.screen === 'equipment') {
      requests.push({
        key: 'ownerPartyId', endpoint: '/m03/parties?role=Supplier',
        label: row => String(row['legalName'] ?? ''), id: 'partyId',
      });
    }
    for (const request of requests) {
      this.api.get<Record<string, unknown>[]>(request.endpoint).pipe(
        catchError(() => of([])),
      ).subscribe((rows) => {
        this.lookups[request.key] = rows.map(row => ({
          id: String(row[request.id] ?? ''),
          label: `${request.label(row)} · ${String(row[request.id] ?? '')}`,
        })).filter(option => !!option.id);
      });
    }
  }

  load(): void {
    this.loading = true;
    this.error = '';
    const endpoint = this.config.endpoint.startsWith('/m03/parties?') && this.searchText.trim()
      ? `${this.config.endpoint}&search=${encodeURIComponent(this.searchText.trim())}`
      : this.config.endpoint;
    this.api.get<unknown>(endpoint).pipe(
      catchError((error: { error?: { detail?: string; title?: string } }) => {
        this.error = error.error?.detail ?? error.error?.title ?? 'Unable to load these masters.';
        return of([]);
      }),
    ).subscribe((response) => {
      if (this.screen === 'bank-accounts' && response && typeof response === 'object') {
        const accounts = (response as { accounts?: Record<string, unknown>[] }).accounts ?? [];
        this.bankVersion = Number((response as { version?: number }).version ?? 0);
        this.rows = accounts;
        this.form['accountsJson'] = JSON.stringify(accounts, null, 2);
      } else {
        this.rows = Array.isArray(response) ? response as Record<string, unknown>[] : [];
      }
      this.loading = false;
    });
  }

  filteredRows(): Record<string, unknown>[] {
    const search = this.searchText.trim().toLocaleLowerCase();
    if (!search) return this.rows;
    return this.rows.filter((row) =>
      Object.values(row).some((value) => String(value ?? '').toLocaleLowerCase().includes(search)));
  }

  loadPriceLines(): void {
    const priceListId = String(this.form['priceListId'] ?? '').trim();
    if (!priceListId) {
      this.error = 'Enter a price list ID before loading its rates.';
      return;
    }
    this.api.get<Record<string, unknown>[]>(`/m03/price-lists/${encodeURIComponent(priceListId)}/lines`).pipe(
      catchError((error: { error?: { detail?: string; title?: string } }) => {
        this.error = error.error?.detail ?? error.error?.title ?? 'Unable to load these price lines.';
        return of([]);
      }),
    ).subscribe((rows) => {
      this.rows = Array.isArray(rows) ? rows : [];
    });
  }

  save(): void {
    if (!this.config.createEndpoint) return;
    this.saving = true;
    this.error = '';
    this.success = '';
    let body: Record<string, unknown>;
    try {
      body = this.makeRequestBody();
    } catch (error) {
      this.error = error instanceof Error ? error.message : 'Check the form values.';
      this.saving = false;
      return;
    }
    let failed = false;
    const request = this.screen === 'bank-accounts'
      ? this.api.put<unknown, unknown>(`${this.config.createEndpoint}?version=${this.bankVersion}`, body['accountsJson'])
      : this.screen === 'price-lines'
        ? this.api.post<unknown, unknown>(
          `/m03/price-lists/${encodeURIComponent(String(body['priceListId']))}/lines`,
          Object.fromEntries(Object.entries(body).filter(([key]) => key !== 'priceListId')),
        )
        : this.editingRow && this.config.updateResource && this.config.updateIdField
          ? this.api.put<unknown, unknown>(
            this.config.updateResource.replace(
              ':id',
              encodeURIComponent(String(this.editingRow[this.config.updateIdField])),
            ),
            body,
          )
          : this.api.post<unknown, unknown>(this.config.createEndpoint, body);
    request.pipe(
      catchError((error: { error?: { detail?: string; title?: string } }) => {
        failed = true;
        this.error = error.error?.detail ?? error.error?.title ?? 'Unable to save the master.';
        this.saving = false;
        return of(null);
      }),
    ).subscribe((result) => {
      if (!failed) {
        this.success = `${this.config.title} record saved.`;
        this.form = {};
        this.editingRow = null;
        this.load();
      }
      this.saving = false;
    });
  }

  loadCustomer360(): void {
    const id = String(this.form['partyId'] ?? '').trim();
    if (!id) return;
    this.loading = true;
    this.error = '';
    this.api.get<Customer360Response>(`/m03/customers/${encodeURIComponent(id)}/360`).pipe(
      catchError((error: { error?: { detail?: string; title?: string } }) => {
        this.error = error.error?.detail ?? error.error?.title ?? 'Unable to load the customer view.';
        return of(null);
      }),
    ).subscribe((result) => {
      this.customer360 = result;
      this.loading = false;
    });
  }

  deactivate(row: Record<string, unknown>): void {
    const resource = this.config.deactivateResource;
    const idField = this.config.deactivateIdField;
    if (!resource || !idField || !window.confirm(`Deactivate this ${this.config.title.toLowerCase()} record?`)) return;
    const id = String(row[idField] ?? '');
    if (!id) return;
    const endpoint = resource.replace(':id', encodeURIComponent(id)) +
      (row['version'] === undefined ? '' : `?version=${encodeURIComponent(String(row['version']))}`);
    this.saving = true;
    this.error = '';
    let failed = false;
    this.api.post<unknown, Record<string, never>>(endpoint, {}).pipe(
      catchError((error: { error?: { detail?: string; title?: string } }) => {
        failed = true;
        this.error = error.error?.detail ?? error.error?.title ?? 'Unable to deactivate this master.';
        this.saving = false;
        return of(null);
      }),
    ).subscribe((result) => {
      if (!failed) {
        this.success = 'Master deactivated. Existing history and references are retained.';
        this.load();
      }
      this.saving = false;
    });
  }

  edit(row: Record<string, unknown>): void {
    this.editingRow = row;
    this.formOpen = true;
    this.error = '';
    this.success = '';
    this.form = {};
    for (const field of this.config.fields) {
      let value = row[field.key];
      if (this.screen === 'plants' && field.key === 'grades' && typeof row['gradesJson'] === 'string') {
        try {
          value = JSON.parse(row['gradesJson'] as string);
        } catch {
          value = [];
        }
      }
      this.form[field.key] = field.type === 'json' && typeof value === 'object'
        ? JSON.stringify(value, null, 2)
        : value ?? '';
    }
  }

  cancelEdit(): void {
    this.editingRow = null;
    this.form = {};
  }

  downloadTemplate(): void {
    if (!this.config.importType) return;
    this.api.getBlob(`/m03/imports/templates/${this.config.importType}`).subscribe((blob) => {
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement('a');
      anchor.href = url;
      anchor.download = `${this.config.importType}-template.csv`;
      anchor.click();
      URL.revokeObjectURL(url);
    });
  }

  selectImport(event: Event): void {
    this.importFile = (event.target as HTMLInputElement).files?.[0] ?? null;
    this.importPreview = null;
  }

  previewImport(): void {
    if (!this.importFile || !this.config.importType) return;
    this.error = '';
    this.success = '';
    this.saving = true;
    const formData = new FormData();
    formData.append('file', this.importFile);
    this.http.post<{
      batchId: string;
      validRows: number;
      errorRows: { rowNumber: number; message: string }[];
      duplicateWarnings: unknown[];
    }>(
      `${this.baseUrl.replace(/\/$/, '')}/m03/imports/${this.config.importType}/preview`, formData,
    ).pipe(catchError((error: { error?: { detail?: string; title?: string } }) => {
      this.error = error.error?.detail ?? error.error?.title ?? 'Unable to preview the import file.';
      this.saving = false;
      return of(null);
    })).subscribe((preview) => {
      this.importPreview = preview;
      this.saving = false;
    });
  }

  confirmImport(): void {
    if (!this.importPreview) return;
    this.saving = true;
    this.error = '';
    this.api.post<unknown, { duplicateHandling: string }>(
      `/m03/imports/${this.importPreview.batchId}/confirm`,
      { duplicateHandling: this.duplicateHandling },
    ).pipe(catchError((error: { error?: { detail?: string; title?: string } }) => {
      this.error = error.error?.detail ?? error.error?.title ?? 'Unable to confirm the import batch.';
      this.saving = false;
      return of(null);
    })).subscribe((result) => {
      if (result !== null) {
        this.success = 'The valid import rows were applied. The batch can be undone for 24 hours if no conflicting edits occurred.';
        this.confirmedBatchId = this.importPreview?.batchId ?? null;
        this.importPreview = null;
        this.load();
      }
      this.saving = false;
    });
  }

  undoImport(): void {
    if (!this.confirmedBatchId) return;
    this.saving = true;
    this.error = '';
    this.api.post<unknown, Record<string, never>>(`/m03/imports/${this.confirmedBatchId}/undo`, {}).pipe(
      catchError((error: { error?: { detail?: string; title?: string } }) => {
        this.error = error.error?.detail ?? error.error?.title ?? 'Unable to undo this import batch.';
        this.saving = false;
        return of(null);
      }),
    ).subscribe((result) => {
      if (result !== null) {
        this.success = 'Import batch undone. Any row changed after import is protected by concurrency checks.';
        this.confirmedBatchId = null;
        this.load();
      }
      this.saving = false;
    });
  }

  humanize(value: string): string {
    return value.replace(/([A-Z])/g, ' $1').replace(/^./, (letter) => letter.toUpperCase());
  }

  cell(value: unknown): string {
    if (value === null || value === undefined) return '—';
    if (typeof value === 'object') return JSON.stringify(value);
    return String(value);
  }

  private makeRequestBody(): Record<string, unknown> {
    const body: Record<string, unknown> = {};
    for (const field of this.config.fields) {
      const raw = this.form[field.key];
      if (raw === undefined || raw === '') continue;
      if (field.type === 'number') body[field.key] = Number(raw);
      else if (field.type === 'json') {
        try {
          const parsed: unknown = JSON.parse(String(raw));
          body[field.key] = ['categoryAttributesJson', 'tagsJson'].includes(field.key)
            ? JSON.stringify(parsed)
            : parsed;
        } catch {
          throw new Error(`${field.label} must be valid JSON.`);
        }
      } else body[field.key] = raw;
    }
    if (this.screen === 'customers' || this.screen === 'suppliers') {
      body['partyType'] = this.editingRow?.['partyType'] ?? 'Business';
      body['creditLimit'] ??= 0;
      body['creditDays'] ??= 0;
      body['creditPolicy'] ??= 'Warn';
      body['creditStatus'] ??= 'Active';
      body['isMsme'] ??= false;
      body['roles'] = this.editingRow
        ? (this.editingRow['roles'] as string[] ?? [])
        : this.screen === 'customers' ? ['Customer'] : ['Supplier'];
      body['version'] = this.editingRow?.['version'] ?? 0;
    } else if (this.screen === 'sites') {
      body['status'] = 'Active';
      body['version'] = this.editingRow?.['version'] ?? 0;
    } else if (this.screen === 'contacts') body['version'] = this.editingRow?.['version'] ?? 0;
    else if (this.screen === 'products') {
      body['version'] = this.editingRow?.['version'] ?? 0;
      body['categoryAttributesJson'] ??= '{}';
      body['qualityReference'] ??= '';
    } else if (this.screen === 'price-lists') body['version'] = this.editingRow?.['version'] ?? 0;
    else if (this.screen === 'vehicles') body['version'] = 0;
    else if (this.screen === 'drivers' || this.screen === 'equipment') body['version'] = this.editingRow?.['version'] ?? 0;
    else if (this.screen === 'payment-terms' || this.screen === 'charge-types') body['version'] = this.editingRow?.['version'] ?? 0;
    else if (this.screen === 'plants') {
      body['version'] = this.editingRow?.['version'] ?? 0;
      if (!body['grades']) body['grades'] = [];
    } else if (this.screen === 'price-lines') body['version'] = this.editingRow?.['version'] ?? 0;
    else if (this.screen === 'rmc-rate-cards' || this.screen === 'tax' || this.screen === 'unit-conversions')
      body['version'] = this.editingRow?.['version'] ?? 0;
    else if (this.screen === 'category-tax-overrides') body['version'] = 0;
    else if (this.screen === 'bank-accounts' && !Array.isArray(body['accountsJson'])) {
      const value = body['accountsJson'];
      try {
        body['accountsJson'] = JSON.parse(String(value ?? '[]'));
      } catch {
        throw new Error('Account configuration must be a valid JSON array.');
      }
      if (!Array.isArray(body['accountsJson'])) throw new Error('Account configuration must be a JSON array.');
    }
    return body;
  }
}
