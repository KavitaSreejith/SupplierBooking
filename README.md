
# Supplier Booking Availability Calculator

## Overview

This application calculates the next available date a supplier can take bookings. The logic takes into account public holidays, multi-day holiday sequences (such as Easter), and cutoff times that restrict bookings after certain dates.

It is built as a class library with xUnit test coverage and a console interface for interactive testing and demonstration.

---

## Business Rules

### 1. Suppliers are unavailable on public holidays

Any date listed as a public holiday is automatically unavailable for bookings.

### 2. Suppliers are also unavailable on the next business day after a public holiday if:

- The booking request is made **on or after 12:00pm AEST**,  
- **Two business days before** the public holiday (or the start of a holiday sequence)

This logic also applies to **multi-day holidays** or **long weekends**, meaning if a public holiday spans several days, the same blackout rules apply after the sequence ends.

---

## Example: Easter 2025

**Holiday sequence:**

- Friday, 18 April 2025 (Good Friday)  
- Saturday, 19 April 2025  
- Sunday, 20 April 2025  
- Monday, 21 April 2025 (Easter Monday)

**Cutoff:**  
- Two business days before Friday, 18 April → **Wednesday, 16 April at 12:00pm AEST**

**Expected behavior:**

| Current Time                  | Next Available Date |
|------------------------------|---------------------|
| Tue 15 April 2025, 9:00am     | Wed 16 April        |
| Wed 16 April 2025, 11:59am    | Tue 22 April        |
| Wed 16 April 2025, 12:00pm    | Wed 23 April        |
| Thu 17 April 2025, 9:00am     | Wed 23 April        |

---

## Functional Requirements

- Input: A `ZonedDateTime` representing the request time
- Output: A `LocalDate` representing the next available booking date
- The logic:
  - Recognizes public holidays
  - Calculates a dynamic cutoff datetime
  - Prevents bookings on public holidays
  - May block the day after holidays if the request comes after the cutoff
  - Considers multi-day holiday spans as a single sequence

---

## Project Structure

- `SupplierBooking` – The core business logic and domain classes
- `SupplierBooking.Tests` – xUnit unit test project with full coverage of scenarios
- `SupplierBooking.ConsoleApp` – A console interface for interactive testing and simulation

---

## How to Run the Console Application

From the solution root:

```bash
dotnet run --project SupplierBooking.ConsoleApp
```

### Console Menu Options

```
1. Check next available date using current time
2. Check next available date using specified time
3. Run test cases
4. Run direct test implementation
5. Exit
```

### Option 3 – Run test cases

This option runs a set of predefined test cases hardcoded in `Program.cs` under the `RunTestCasesAsync` method. These correspond to important scenarios such as:

- Booking before the cutoff for a holiday
- Booking just before or at the cutoff
- Booking after the cutoff
- Booking that overlaps a holiday sequence

Each test prints:

- The reference datetime
- The expected and actual next available booking date
- Whether the cutoff logic was triggered
- The list of affected holidays
- Whether the test passed or failed

### Option 4 – Run direct test implementation

This runs a method called `TestHelper.RunTestCasesStandaloneAsync()`. It is designed for development and debugging purposes. It may run additional tests or alternative logic to validate that the business rules are still upheld.

Use this option when:

- You are extending the logic and want to verify more complex sequences
- You are troubleshooting logic failures or regressions

---

## How to Run Unit Tests (xUnit)

Unit tests are located in the `SupplierBooking.Tests` project. To run all tests:

```bash
dotnet test SupplierBooking.Tests
```

These tests include:

- Tests for known edge cases (Easter 2025 scenarios)
- Isolated holiday tests
- Multi-day sequence handling
- Cutoff logic validation
- Scenarios without any holidays

Each test verifies that the correct `NextAvailableDate` is returned and whether the cutoff was correctly applied.

---

## Extensibility

To add new test scenarios:

- Update or add to the xUnit test project (`AvailabilityCalculatorTests.cs`)
- Or, for interactive testing, add new entries to the `testCases` array in `Program.cs > RunTestCasesAsync`

To support real-world dynamic holiday data, implement a new `IPublicHolidayProvider` that pulls data from an external API or database.

---