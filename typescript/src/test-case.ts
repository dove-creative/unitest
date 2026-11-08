interface CaseRule {
    readonly include: boolean;
    readonly definitions: readonly unknown[];
}

export class TestCase {
    // Front
    public get count(): number { return this._rules.length; }

    // Internal
    private _rules: readonly CaseRule[];


    // Content
    public constructor(...definitions: unknown[]) {
        this._rules = definitions.map((definition) => ({ include: true, definitions: [definition] }));
    }

    public static fromRules(rules: Iterable<CaseRule>): TestCase {
        const testCase = new TestCase();
        testCase._rules = Array.from(rules, (rule) => ({ include: rule.include, definitions: [...rule.definitions] }));
        return testCase;
    }

    public at(index: number): unknown {
        this.validateIndex(index, this.count - 1);
        const rule = this._rules[index];

        if (!rule.include || rule.definitions.length !== 1) {
            throw new Error("The test case at this index must be a single confined definition.");
        }

        return rule.definitions[0];
    }

    public append(definition: unknown, include = true): TestCase {
        return this.withRule(this.count, { include, definitions: [definition] });
    }

    public confine(index: number, definition: unknown): TestCase {
        this.validateIndex(index);
        validateDefinitions([definition]);
        return this.withRule(index, { include: true, definitions: [definition] });
    }

    public include(index: number, ...definitions: unknown[]): TestCase {
        return this.set(index, definitions, true);
    }

    public exclude(index: number, ...definitions: unknown[]): TestCase {
        return this.set(index, definitions, false);
    }

    private set(index: number, definitions: unknown[], include: boolean): TestCase {
        this.validateIndex(index);
        validateDefinitions(definitions);
        const current = this._rules[index];

        if (!current) return this.withRule(index, { include, definitions });

        const values = new Set(current.definitions);

        for (const definition of definitions) {
            if (include === current.include) values.add(definition);
            else values.delete(definition);
        }

        return this.withRule(index, { include: current.include, definitions: [...values] });
    }

    public confineable(index: number, ...definitions: unknown[]): boolean {
        this.validateIndex(index, Number.MAX_SAFE_INTEGER);
        validateDefinitions(definitions);
        const rule = this._rules[index];

        if (!rule) return true;

        return definitions.every((definition) => rule.definitions.includes(definition) === rule.include);
    }

    public tryConfine(index: number, definition: unknown): TestCase | null {
        return this.confineable(index, definition) ? this.confine(index, definition) : null;
    }

    public confineableExcept(index: number, ...definitions: unknown[]): TestCase | null {
        this.validateIndex(index);
        validateDefinitions(definitions);
        const current = this._rules[index];

        if (current?.include) {
            const remaining = [...new Set(current.definitions.filter((definition) => !definitions.includes(definition)))];
            return remaining.length === 0 ? null : this.withRule(index, { include: true, definitions: remaining });
        }

        return this.withRule(index, {
            include: false,
            definitions: current ? [...new Set([...current.definitions, ...definitions])] : definitions,
        });
    }

    private withRule(index: number, rule: CaseRule): TestCase {
        const result = new TestCase();
        const rules = [...this._rules];
        rules[index] = rule;
        result._rules = rules;
        return result;
    }

    private validateIndex(index: number, maximum = this.count): void {
        if (!Number.isInteger(index) || index < 0 || index > maximum) {
            throw new RangeError(`Index ${index} must be in range [0..${maximum}].`);
        }
    }
}

function validateDefinitions(definitions: readonly unknown[]): void {
    if (definitions.length === 0 || definitions.some((definition) => definition == null)) {
        throw new TypeError("Definitions must not be empty or contain null or undefined elements.");
    }
}
