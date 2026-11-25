import { documents, forms, steps, titles } from './setup-fields';
describe('Onboarding screens', () => {
  it('provides a named destination for every wizard step', () => {
    expect(steps.length).toBe(8);
    for (const step of steps) expect(titles[step]).toBeTruthy();
  });
  it('offers the six document types in branch numbering', () => {
    expect(forms['numbering'][1].fields.find((f) => f.key === 'document')?.options).toEqual(
      documents,
    );
    expect(new Set(documents).size).toBe(6);
  });
});
