/**
 * Formats the date to a string in the format of 'Mar 04, 2019, 22:29:59 UTC'.
 */
export const formatDateTime: (date: Date | number) => string = date => {
	const d = typeof date === "number" ? new Date(date) : date;

	return d.toLocaleString("en-US", {
		month: "short",
		day: "2-digit",
		year: "numeric",
		hour12: false,
		hour: "2-digit",
		minute: "2-digit",
		second: "2-digit",
		timeZoneName: "short",
		timeZone: "UTC"
	});
};
