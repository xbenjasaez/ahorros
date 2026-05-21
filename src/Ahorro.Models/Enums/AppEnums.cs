namespace Ahorro.Models.Enums;

public enum PeriodFrequency { Monthly, Biweekly }

public enum IncomeFrequency { Monthly, Biweekly, OneTime }

public enum IncomeType { Salary, Freelance, Investment, Other }

public enum AllocationMode { Percentage, FixedAmount, Manual }

public enum BudgetGroup { Needs, Wants, Savings, Other }

public enum TransactionType { Income, Expense, DebtPayment, Adjustment, InternalTransfer }

public enum TransactionStatus { Pending, Paid, Cancelled }

public enum BudgetLineStatus { Normal, Attention, Limit, Exceeded }

public enum ScheduledPaymentStatus { Pending, Upcoming, Paid, Overdue }

public enum DebtStatus { Active, PaidOff, Paused }

public enum GoalStatus { Active, Completed, Archived }

public enum PaymentMethodType { Cash, Debit, Credit, Transfer }

public enum ExportType { Transactions, Budget, Report, Goals }

public enum NavigationPage { Dashboard, Budget, Transactions, Goals, Payments, Reports, Settings }
