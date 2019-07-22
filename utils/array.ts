/**
 * Finds the index of @param alias in @param input while ignoring case. If @param alias is an array of potential matches, this function will find the index of the first one to match.
 */
export const findIndexIgnoreCase = (input: string[], ...matches: string[]) =>
	matches.reduce((index, alias) => {
		if (index > -1) return index;

		return input.findIndex(arg => arg.toLowerCase() === alias.toLowerCase());
	}, -1);

/**
 * Filters the @param input removing any duplicate items with the same value according to @param prop.
 */
export const unique = <T, K extends keyof T>(input: T[], prop: K | ((item: T) => any)) => {
	const fn: (item: T) => any = typeof prop === "function" ? prop : item => item[prop];

	const map = input.reduce<Map<any, T>>((state, item) => {
		const propValue = fn(item);

		return state.has(propValue) ? state : state.set(propValue, item);
	}, new Map<any, T>());

	return Array.from(map.values());
};
