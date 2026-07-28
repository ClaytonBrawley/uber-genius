# Uber Genius - MVP

## Goal

The first release focuses on helping Uber drivers understand their performance by transforming raw Uber trip history into meaningful analytics.

The MVP should provide drivers with a clearer understanding of their earnings, trips, and driving patterns than the standard Uber Driver app. It represents Phase 1 (Web Analytics) of the product roadmap and focuses on presenting Uber trip data more effectively through interactive analytics, visualizations, and insights.

---

## MVP Features

### 1. Uber Data Import

Users can upload their Uber trip history export file and import their data into the platform.

#### Features

- Upload Uber CSV/export file
- Validate file format
- Parse trip records
- Display import summary

---

### 2. Trip History Table

Users can explore their complete trip history through a searchable and filterable table.

#### Data Displayed

- Date
- Pickup location
- Drop-off location
- Trip Distance
- Trip times
- Earnings

#### Features

- Search trips
- Filter by date range
- Sort by earnings
- Sort by distance

---

### 3. Earnings Dashboard

Users can quickly understand their overall performance through key metrics.

#### Dashboard Metrics

- Total earnings
- Total trips
- Average earnings per trip
- Estimated hourly earnings
- Total miles
- Average earnings per mile

> Estimated hourly earnings are calculated using the available trip history and may differ from true online earnings.

---

### 4. Earnings Analytics

Users can explore trends and identify patterns in their driving performance.

#### Visualizations

- Earnings over time
- Earnings by day of week
- Earnings by hour

Examples:

**Earnings by Day**

| Day | Hourly Earnings |
|---|---:|
| Friday | $34/hr |
| Saturday | $31/hr |
| Sunday | $24/hr |
| Monday | $19/hr |

**Earnings by Hour**

| Time | Hourly Earnings |
|---|---:|
| 7 PM | $32/hr |
| 8 PM | $29/hr |
| 9 PM | $21/hr |

---

### 5. Trip Map

Users can visualize where trips occurred geographically.

The MVP visualizes trip locations but does not include actual route tracking.

#### Features

- Pickup location markers
- Drop-off location markers

Example:

> "Show me everywhere I picked passengers up."

---

## Out of Scope

To keep the initial release focused, the following features are intentionally excluded from the MVP and planned for future phases:

- Real-time GPS tracking
- Mobile companion application
- Personalized driving recommendations
- Strategy simulations
- Demand prediction
- Automated trip recording
- Community benchmarks

---

## MVP Completion Criteria

The MVP is complete when a user can:

- Upload a valid Uber trip history export
- View imported trips in a searchable table
- Explore summary metrics on a dashboard
- Analyze earnings using charts
- Visualize trip locations on a map

---

## MVP Success Criteria

The MVP is successful if a driver can:

- Upload their Uber trip history
- Understand their earnings performance
- Identify profitable patterns
- Explore where and when they earn the most
- Make better decisions using their own historical data