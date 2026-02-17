# Azure Pricing Calculator - Complete Cost Estimation Guide
## Claims RAG Bot - Azure Services Cost Breakdown

**Calculator URL:** https://azure.microsoft.com/en-in/pricing/calculator/  
**Last Updated:** February 12, 2026  
**Estimated Monthly Total (MVP):** **$306 USD** | **Production:** **$951 USD**

---

## How to Use Azure Pricing Calculator

### Step-by-Step Instructions

1. **Open the Calculator**
   - Visit: https://azure.microsoft.com/en-in/pricing/calculator/
   - Click "Products" tab to add services

2. **Add Each Service Below**
   - Find the service in the product list
   - Configure according to specifications provided
   - Add to estimate

3. **Save & Share**
   - Once all services are added, click "Export" to save
   - Or use "Save" button to get a shareable link

---

## 🎯 MVP Quick Reference (Recommended Starting Point)

### Realistic MVP Assumptions

For a **working MVP** (proof of concept / initial deployment), assume:

- **Claims volume:** 100-200 claims/day (not 600)
- **Testing period:** 3 months
- **Users:** 5-10 internal users
- **Policy documents:** 5,000-10,000 clauses indexed
- **Purpose:** Validate functionality, gather feedback, demonstrate value

### MVP Cost Summary

| Service | MVP Monthly Cost | Notes |
|---------|------------------|-------|
| Azure OpenAI (GPT-4o + Embeddings) | **$95** | Lower volume, use GPT-4o |
| Azure AI Search (Basic) | **$75** | Perfect for MVP scope |
| Azure Cosmos DB (Serverless) | **$25** | Minimal usage |
| Azure Blob Storage | **$5** | 3 GB storage |
| Azure Document Intelligence | **$18** | 18K pages/month |
| Azure Language Service | **$12** | 6K text records |
| Azure Computer Vision | **$3** | 3K images |
| Monitoring & Data Transfer | **$10** | Basic monitoring |
| **MVP TOTAL** | **$243-306/month** | **Realistic starting cost** |

### Scaling Path

```
MVP (100-200 claims/day)     →  $243-306/month
↓
Growth (400-600 claims/day)  →  $506-776/month  
↓
Production (600+ claims/day) →  $776-951/month
```

**💡 Recommendation:** Start with MVP configuration, monitor actual usage for 1 month, then adjust.

---

## Service 1: Azure OpenAI Service

### How to Add
1. In Products tab, search for "**Azure OpenAI**"
2. Click to add to estimate

### Configuration Details

**Region:** East US (or East US 2, South Central US)

**Model 1: Text Embeddings (text-embedding-ada-002)**

| Setting | Value | Notes |
|---------|-------|-------|
| **Model** | text-embedding-ada-002 | Select from dropdown |
| **Monthly Tokens (Input)** | 2,250,000 | 500 requests/day × 150 tokens × 30 days |
| **Pricing** | $0.0001 per 1K tokens | Standard rate |

**Calculation:**
- 2,250,000 tokens ÷ 1,000 × $0.0001 = **$2.25/month**

---

**Model 2: GPT-4 Turbo (Chat Completions)**

| Setting | Value | Notes |
|---------|-------|-------|
| **Model** | GPT-4 Turbo (1106-preview) | Or latest GPT-4 Turbo |
| **Monthly Tokens (Input)** | 27,000,000 | 600 requests/day × 1,500 tokens × 30 days |
| **Monthly Tokens (Output)** | 9,000,000 | 600 requests/day × 500 tokens × 30 days |
| **Input Pricing** | $0.01 per 1K tokens | Standard rate |
| **Output Pricing** | $0.03 per 1K tokens | Standard rate |

**Calculation:**
- Input: 27M ÷ 1,000 × $0.01 = $270/month
- Output: 9M ÷ 1,000 × $0.03 = $270/month
- **Total: $540/month**

**Azure OpenAI Total: $2.25 + $540 = $542.25/month**

---

### MVP Configuration (GPT-4o - RECOMMENDED for MVP) ✅

**For MVP with 100-200 claims/day:**

| Setting | Value | Notes |
|---------|-------|-------|
| **Model** | GPT-4o | Newer, faster, cheaper |
| **Monthly Tokens (Input)** | 9,000,000 | 200 requests/day × 1,500 tokens × 30 days |
| **Monthly Tokens (Output)** | 3,000,000 | 200 requests/day × 500 tokens × 30 days |
| **Embedding Tokens** | 750,000 | 200 requests/day × 125 tokens × 30 days |
| **Input Pricing** | $0.005 per 1K tokens | **50% cheaper than GPT-4 Turbo** |
| **Output Pricing** | $0.015 per 1K tokens | **50% cheaper than GPT-4 Turbo** |
| **Embedding Pricing** | $0.0001 per 1K tokens | Standard rate |

**MVP Calculation:**
- Embeddings: 750K ÷ 1,000 × $0.0001 = $0.075/month
- Input: 9M ÷ 1,000 × $0.005 = $45/month
- Output: 3M ÷ 1,000 × $0.015 = $45/month
- **Total MVP with GPT-4o: ~$90-95/month** ✅

**Production Scale (600 claims/day) with GPT-4o:**
- Input: 27M ÷ 1,000 × $0.005 = $135/month
- Output: 9M ÷ 1,000 × $0.015 = $135/month
- Embeddings: 2.25M ÷ 1,000 × $0.0001 = $0.225/month
- **Total Production with GPT-4o: $270-272/month**

---

### Alternative: GPT-4 Turbo (Higher Cost, Premium Performance)

**Only use GPT-4 Turbo if:**
- You need the absolute best reasoning quality
- Cost is not a primary concern
- Complex insurance policy interpretation required

**Production with GPT-4 Turbo:** $542/month (see calculation above)

---

## Service 2: Azure AI Search (Cognitive Search)

### How to Add
1. In Products tab, search for "**Azure Cognitive Search**" or "**Azure AI Search**"
2. Click to add to estimate

### Configuration Details - MVP/Development (RECOMMENDED)

| Setting | Value | Notes |
|---------|-------|-------|
| **Region** | Same as Azure OpenAI | East US recommended |
| **Tier** | **Basic** | **Perfect for MVP** ✅ |
| **Units** | 1 | Single search unit |
| **Hours per month** | 730 | Full month (24/7) |

**What Basic Tier Provides:**
- Storage: 2 GB (you need only 80 MB)
- Documents: 1 million (you need 5-10K)
- Queries: 3 per second (you need ~0.01 qps average)
- ✅ Vector search supported
- ✅ Perfect for MVP/development

**Calculation:**
- Basic: 1 unit × 730 hours × ~$0.103/hour = **$75/month**

**Azure AI Search Total (MVP): $75/month**

---

### Production Alternative (If High Availability Needed)

**For Production with High Availability:**

| Setting | Value | Notes |
|---------|-------|-------|
| **Tier** | Standard S1 | Production-grade |
| **Units** | 1 | Can scale to 36 units max |
| **Hours per month** | 730 | Full month (24/7) |

**What Standard S1 Provides:**
- Storage: 25 GB (overkill for 80 MB)
- Documents: 25 million (overkill for 10K)
- Queries: 180 per second (overkill for 0.01 qps)
- ✅ High availability options
- ✅ More replicas and partitions

**Calculation:**
- Standard S1: 1 unit × 730 hours × ~$0.342/hour = **$250/month**

**Azure AI Search Total (Production): $250/month**

---

### ⚠️ Recommendation for MVP

**Use Basic Tier** for MVP/initial deployment:
- **Cost:** $75/month (saves $175/month vs Standard S1)
- **Sufficient for:** 10K documents, low query volume
- **Upgrade later:** Easy migration to Standard when needed

You're using **less than 1%** of Standard S1 capacity for MVP scope!

---

## Service 3: Azure Cosmos DB

### How to Add
1. In Products tab, search for "**Azure Cosmos DB**"
2. Click to add to estimate

### Configuration Details

| Setting | Value | Notes |
|---------|-------|-------|
| **Region** | Same as application | East US recommended |
| **API** | Core (SQL) | Default API |
| **Capacity Mode** | **Serverless** | **Recommended for low usage** |
| **Single Region Write** | Yes | No multi-region needed |

**Serverless Mode Estimation:**

| Metric | Value | Notes |
|--------|-------|-------|
| **Storage (GB/month)** | 1 GB | 600 claims/day × 2 KB × 30 days ≈ 36 MB |
| **Request Units (RU) consumed** | 180,000 RU/month | 600 writes × 10 RU + 50 reads × 1 RU daily |
| **RU Pricing** | $0.25 per million RU | Serverless rate |

**Calculation:**
- Storage: 1 GB × $0.25 = $0.25/month
- RUs: 0.18M × $0.25 = $0.045/month
- **Minimum billing:** $25/month (serverless has minimum charge)

**Azure Cosmos DB Total: $25/month**

---

### Alternative: Provisioned Throughput (Not Recommended)

**If calculator forces Provisioned mode:**

| Setting | Value | Notes |
|---------|-------|-------|
| **Throughput** | Autoscale: 400-4000 RU/s | Scales automatically |
| **Max RU/s** | 4000 | Maximum capacity |
| **Storage** | 1 GB | Minimal |

**Calculation:**
- Would cost ~$350/month (scales down to ~$25-50 with low usage)
- **Stick with Serverless for predictable costs**

---

## Service 4: Azure Blob Storage

### How to Add
1. In Products tab, search for "**Storage Accounts**"
2. Select "**Blob Storage**" option
3. Click to add to estimate

### Configuration Details

| Setting | Value | Notes |
|---------|-------|-------|
| **Region** | Same as application | East US recommended |
| **Performance** | Standard | General Purpose v2 |
| **Redundancy** | LRS (Locally Redundant) | Cheapest option |
| **Access Tier** | Hot | Frequently accessed |
| **Storage Capacity (GB)** | 10 GB | ~9 GB/month for claim docs |

**Operations (Monthly):**

| Operation Type | Count | Pricing |
|----------------|-------|---------|
| **Write operations** | 21,000 | $0.05 per 10,000 operations |
| **Read operations** | 3,000 | $0.004 per 10,000 operations |

**Calculations:**

| Scenario | Storage | Writes/Mo | Reads/Mo | Monthly Cost |
|----------|---------|-----------|----------|-------------|
| **MVP** | 3 GB | 6,000 | 1,000 | $5/month |
| **Growth** | 6 GB | 12,000 | 2,000 | $5/month |
| **Production** | 10 GB | 21,000 | 3,000 | $5/month |

**Calculation (all tiers similar due to low costs):**
- Storage: 3-10 GB × $0.018 = $0.05-0.18/month
- Operations: $0.10-0.20/month
- Data transfer: ~$4-5/month (outbound bandwidth)
- **Total: ~$5/month** (minimal variation)

**Azure Blob Storage Total: $5/month (consistent across MVP and Production)**

---

## Service 5: Azure AI Document Intelligence

### How to Add
1. In Products tab, search for "**Document Intelligence**" or "**Form Recognizer**"
2. Click to add to estimate

### Configuration Details

| Setting | Value | Notes |
|---------|-------|-------|
| **Region** | Any region | East US recommended |
| **Model** | **Prebuilt Read** | Optimized for text extraction |
| **Pages Processed/Month** | 54,000 | 600 docs/day × 3 pages × 30 days |

**Pricing Tiers:**

| Model Type | Price per 1K Pages | Your Cost |
|------------|-------------------|-----------|
| **Prebuilt Read** | $1.00 | 54 × $1 = **$54/month** ✅ Recommended |
| Prebuilt Document | $10.00 | 54 × $10 = $540/month (if need tables) |
| Prebuilt Layout | $10.00 | 54 × $10 = $540/month (if need structure) |

**Calculations:**

| Scenario | Docs/Day | Pages/Doc | Monthly Pages | Cost |
|----------|----------|-----------|---------------|------|
| **MVP** | 200 | 3 | 18,000 | $18/month |
| **Growth** | 400 | 3 | 36,000 | $36/month |
| **Production** | 600 | 3 | 54,000 | $54/month |

**MVP:** 200 docs × 3 pages × 30 days = 18,000 pages → $18/month  
**Production:** 600 docs × 3 pages × 30 days = 54,000 pages → $54/month

**Azure Document Intelligence Total: $18/month (MVP) | $54/month (Production)**

⚠️ **Note:** If you need table/form extraction (Prebuilt Document model), costs increase 10x. Use Prebuilt Read for plain text only.

---

## Service 6: Azure AI Language Service

### How to Add
1. In Products tab, search for "**Language Service**" or "**Text Analytics**"
2. Click to add to estimate

### Configuration Details

| Setting | Value | Notes |
|---------|-------|-------|
| **Region** | Any region | East US recommended |
| **Pricing Tier** | Standard (S) | Pay-as-you-go |
| **Feature** | Named Entity Recognition (NER) | Primary feature |

**Monthly Usage:**

| Feature | Text Records/Month | Pricing | Cost |
|---------|-------------------|---------|------|
| **NER** | 18,000 | $2 per 1,000 records | $36/month |
| **Key Phrase Extraction** | 18,000 | Included with NER | $0 |

**Calculations:**

| Scenario | Claims/Day | Monthly Records | Cost |
|----------|-----------|-----------------|------|
| **MVP** | 100-200 | 3,000-6,000 | $6-12/month |
| **Growth** | 400 | 12,000 | $24/month |
| **Production** | 600 | 18,000 | $36/month |

**MVP:** 200 claims/day × 30 days = 6,000 records → $12/month  
**Production:** 600 claims/day × 30 days = 18,000 records → $36/month

**Azure Language Service Total: $12/month (MVP) | $36/month (Production)**

---

## Service 7: Azure Computer Vision

### How to Add
1. In Products tab, search for "**Computer Vision**" or "**Azure AI Vision**"
2. Click to add to estimate

### Configuration Details

| Setting | Value | Notes |
|---------|-------|-------|
| **Region** | Any region | East US recommended |
| **Pricing Tier** | Standard S1 | Standard tier |
| **Feature** | Image Analysis | Object detection, tags, description |

**Monthly Usage:**

| Scenario | Images/Day | Monthly Total | Cost |
|----------|-----------|---------------|------|
| **MVP** | 100 | 3,000 | $3/month |
| **Growth** | 200 | 6,000 | $6/month |
| **Production** | 300 | 9,000 | $9/month |

**MVP Calculation:**
- 100 images/day × 30 days = 3,000 images
- 3,000 ÷ 1,000 × $1 = **$3/month**

**Production Calculation:**
- 300 images/day × 30 days = 9,000 images
- 9,000 ÷ 1,000 × $1 = **$9/month**

**Azure Computer Vision Total: $3/month (MVP) | $9/month (Production)**

---

## Service 8: Additional Services (Optional but Recommended)

### Azure Monitor + Log Analytics

**For monitoring and diagnostics**

| Setting | Value | Cost |
|---------|-------|------|
| **Log Ingestion** | 5 GB/month | $2.76 per GB |
| **Log Retention** | 30 days | Included (first 31 days free) |
| **Metrics** | Standard | Included |

**Calculation:**
- 5 GB × $2.76 = $13.80/month
- Add alerts, dashboards: +$5/month
- **Total: ~$20/month**

---

### Data Transfer (Bandwidth)

**Egress charges for data leaving Azure**

| Setting | Value | Cost |
|---------|-------|------|
| **Outbound Transfer** | 10 GB/month | First 100 GB: $0.087/GB |
| **Region** | Within region | Free |

**Calculation:**
- 10 GB × $0.087 = $0.87/month
- Round up to **$10/month** (buffer for spikes)

---

## Complete Cost Summary Table

### 🎯 MVP Configuration (100-200 claims/day) - RECOMMENDED START

| # | Azure Service | Monthly Cost | 2-Month Cost | 3-Month Cost |
|---|---------------|--------------|--------------|--------------|  
| 1 | **Azure OpenAI** (GPT-4o + Embeddings) ✅ | $90-95 | $180-190 | $270-285 |
| 2 | **Azure AI Search** (Basic) ✅ | $75 | $150 | $225 |
| 3 | **Azure Cosmos DB** (Serverless) | $25 | $50 | $75 |
| 4 | **Azure Blob Storage** (Hot, LRS) | $5 | $10 | $15 |
| 5 | **Azure Document Intelligence** (Prebuilt Read) | $18 | $36 | $54 |
| 6 | **Azure Language Service** (NER + Key Phrases) | $12 | $24 | $36 |
| 7 | **Azure Computer Vision** (Image Analysis) | $3 | $6 | $9 |
| 8 | **Azure Monitor + Log Analytics** | $10 | $20 | $30 |
| 9 | **Data Transfer (Egress)** | $5 | $10 | $15 |
| | | | | |
| | **MVP TOTAL** | **$243-248** | **$486-496** | **$729-744** |

**💡 Best for:** Proof of concept, initial testing, 5-10 users, 100-200 claims/day

---

### 📈 Growth Configuration (400-600 claims/day)

| # | Azure Service | Monthly Cost | 2-Month Cost | 3-Month Cost |
|---|---------------|--------------|--------------|--------------|  
| 1 | **Azure OpenAI** (GPT-4o + Embeddings) | $180-272 | $360-544 | $540-816 |
| 2 | **Azure AI Search** (Basic or Standard S1) | $75-250 | $150-500 | $225-750 |
| 3 | **Azure Cosmos DB** (Serverless) | $25 | $50 | $75 |
| 4 | **Azure Blob Storage** (Hot, LRS) | $5 | $10 | $15 |
| 5 | **Azure Document Intelligence** (Prebuilt Read) | $36-54 | $72-108 | $108-162 |
| 6 | **Azure Language Service** (NER + Key Phrases) | $24-36 | $48-72 | $72-108 |
| 7 | **Azure Computer Vision** (Image Analysis) | $6-9 | $12-18 | $18-27 |
| 8 | **Azure Monitor + Log Analytics** | $15-20 | $30-40 | $45-60 |
| 9 | **Data Transfer (Egress)** | $8-10 | $16-20 | $24-30 |
| | | | | |
| | **GROWTH TOTAL** | **$374-681** | **$748-1,362** | **$1,122-2,043** |

**💡 Best for:** Pilot deployment, 20-50 users, validated use case

## Cost Optimization Summary

### Configuration Comparison

| Configuration | Monthly Cost | Use Case |
|---------------|--------------|----------|
| **MVP (Recommended)** | **$776** | Basic tier AI Search, GPT-4 Turbo |
| **MVP + Optimized AI** | **$506** | Basic tier AI Search, GPT-4o instead of GPT-4 Turbo |
| **Production** | **$951** | Standard S1 AI Search, GPT-4 Turbo |
| **Production + Optimized AI** | **$681** | Standard S1 AI Search, GPT-4o |

### Optimization Options

| Change | Original Cost | Optimized Cost | Savings | Recommended For |
|--------|--------------|----------------|---------|-----------------|
| Use **Basic** AI Search (MVP) ✅ | $250/mo | $75/mo | **-$175/mo** | MVP/Development |
| Use **GPT-4o** instead of GPT-4 Turbo | $542/mo | $272/mo | **-$270/mo** | Cost-sensitive deployments |
| Use **Prebuilt Read** (already selected) | $54/mo | $54/mo | $0 | Already optimized ✅ |
| **Cosmos DB Serverless** (already selected) | $25/mo | $25/mo | $0 | Already optimized ✅ |
| | | | | |
| **Maximum Savings** | $951/mo | **$506/mo** | **-$445/mo** | Basic + GPT-4o |
| 9 | **Data Transfer (Egress)** | $10 | $20 | $30 |
| | | | | |
| | **PRODUCTION TOTAL** | **$951** | **$1,902** | **$2,853** |

---

## Cost Optimization Summary

### Deployment Strategy Comparison

| Configuration | Monthly Cost | Use Case | Savings vs Production |
|---------------|--------------|----------|----------------------|
| **MVP** (GPT-4o + Basic Search) | **$243-248** | Initial testing, POC | **-$703** (-74%) |
| **Growth** (GPT-4o + Basic Search) | **$374-431** | Pilot, 400 claims/day | **-$520-577** (-60-65%) |
| **Growth** (GPT-4o + Standard S1) | **$549-681** | Pilot with HA needs | **-$270-402** (-28-42%) |
| **Production** (GPT-4o + Standard S1) | **$681** | Full production (optimized) | **-$270** (-28%) |
| **Production** (GPT-4 Turbo + Standard S1) | **$951** | Premium AI performance | Baseline |

### Key Optimization Levers

| Optimization | Savings | When to Apply |
|--------------|---------|---------------|
| Use **GPT-4o** instead of GPT-4 Turbo | **-$270/mo** (50%) | Default choice unless premium reasoning needed |
| Use **Basic** AI Search instead of Standard S1 | **-$175/mo** (70%) | MVP/testing (upgrade when >50K docs or >5 qps) |
| Start with **lower volume** (200 vs 600 claims/day) | **-$150-200/mo** | MVP phase, scale as demand grows |
| Use **Prebuilt Read** vs Prebuilt Document | **-$486/mo** (90%) | When tables/forms not critical ✅ Already optimized |
| **Cosmos DB Serverless** vs Provisioned | **-$325/mo** | When <1000 claims/day ✅ Already optimized |---

2. ✅ **Azure Cognitive Search / AI Search**
   - **Basic tier: 1 unit, 730 hours/month** (MVP - RECOMMENDED) ✅
   - Or Standard S1: 1 unit, 730 hours/month (Production)ing Calculator

1. ✅ **Azure OpenAI**
   - text-embedding-ada-002: 2.25M tokens/month
   - GPT-4 Turbo: 27M input + 9M output tokens/month
   - **Or GPT-4o for 50% savings**

2. ✅ **Azure Cognitive Search / AI Search**
   - Standard S1, 1 replica, 1 partition
   - **Or Basic tier for dev/test**

3. ✅ **Azure Cosmos DB**
   - Serverless mode, 1 GB storage, 180K RU/month

4. ✅ **Storage Account (Blob Storage)**
   - Standard, Hot, LRS, 10 GB storage
   - 21K write ops, 3K read ops

5. ✅ **Azure Document Intelligence**
   - Prebuilt Read model, 54K pages/month

6. ✅ **Azure Language Service**
   - Standard S, NER feature, 18K text records/month

7. ✅ **Azure Computer Vision**
   - Standard S1, Image Analysis, 9K transactions/month

8. ✅ **Azure Monitor (Optional)**
   - 5 GB log ingestion, 30-day retention

9. ✅ **Data Transfer (Optional)**
   - 10 GB outbound data transfer

---

## Expected Calculator Results

### 🎯 MVP - Realistic Starting Point (100-200 claims/day)
```
Total Estimated Monthly Cost: $243-248 USD ✅ RECOMMENDED
Total Estimated 2-Month Cost: $486-496 USD
Total Estimated 3-Month Cost: $729-744 USD

Configuration:
- GPT-4o (not GPT-4 Turbo)
- Basic AI Search (not Standard S1)  
- 200 claims/day (not 600)
- All services in Serverless/optimized tiers
```

### 📈 Growth - Scaling Up (400-600 claims/day)
```
Total Estimated Monthly Cost: $374-681 USD
Total Estimated 2-Month Cost: $748-1,362 USD
Total Estimated 3-Month Cost: $1,122-2,043 USD

Configuration:
- GPT-4o recommended
- Basic or Standard S1 AI Search (based on query load)
- 400-600 claims/day
```

### 🚀 Production - Full Scale (600+ claims/day)
```
Total Estimated Monthly Cost (Optimized): $681 USD
Total Estimated Monthly Cost (Premium): $951 USD

Optimized: GPT-4o + Standard S1
Premium: GPT-4 Turbo + Standard S1
```

---

## Regional Pricing Notes

### Azure OpenAI Availability by Region
- ✅ **East US** - GPT-4 Turbo available
### If Your Total is ~$240-250/month
✅ **Perfect MVP Start!** This is the recommended realistic MVP:
- GPT-4o (cost-optimized AI)
- Basic AI Search (MVP tier)
- 200 claims/day volume
- All services optimized

### If Your Total is ~$370-430/month
✅ **Good Growth Configuration!** Scaling up appropriately:
- GPT-4o (cost-optimized)
- Basic or Standard AI Search
- 400-600 claims/day volume

### If Your Total is ~$680-700/month
✅ **Production Optimized!** Smart production setup:
- GPT-4o (balanced cost/performance)
- Standard S1 AI Search (production)
- 600+ claims/day

### If Your Total is ~$950-1000/month
✅ **Premium Production!** Maximum performance:
- GPT-4 Turbo (best reasoning)
- Standard S1 AI Search
- Full production scale
---

## How to Interpret Your Calculator Results

### If Your Total is ~$950-1000/month
✅ **Correct!** This matches the baseline configuration with:
- GPT-4 Turbo (premium pricing)
- Standard S1 AI Search
- All recommended services

### If Your Total is ~$450-500/month
✅ **Optimized!** You've applied cost-saving measures:
- Using GPT-4o instead of GPT-4 Turbo
- Or using Basic AI Search tier for development

### If Your Total is Much Higher (>$1,500/month)
⚠️ **Check these common mistakes:**
- Using "Prebuilt Document" ($10/1K pages) instead of "Prebuilt Read" ($1/1K pages)
- Cosmos DB in Provisioned mode instead of Serverless
- AI Search with Semantic Search add-on (+$500/month)
- Multiple regions selected (increases costs)

### If Your Total is Much Lower (<$400/month)
⚠️ **You may be missing:**
- Azure OpenAI costs (largest component)
- AI Search service
- Proper usage volumes (pages, tokens, etc.)

---

## Next Steps After Calculator

### 1. Save Your Estimate
- Click "**Export**" → Download Excel/PDF
- Click "**Save**" → Get shareable link
- Email link to stakeholders

### 2. Review with Team
- Share calculator link for approval
- Discuss optimizations (GPT-4o vs GPT-4 Turbo)
- Decide on dev vs production tiers

### 3. Set Up Budget Alerts
- In Azure Portal: Cost Management → Budgets
- Set monthly budget: $1,000 USD
- Alert at: 80% ($800), 90% ($900), 100% ($1,000)

### 4. Start Deployment
- Follow `AZURE_PORTAL_SETUP_GUIDE.md` for step-by-step provisioning
- Use `AZURE_SERVICES_GUIDE.md` for configuration details

---

## Support & Questions

### Common Issues

**Q: Calculator shows different prices for OpenAI models**
A: Prices vary by region. Select "East US" for standard rates.

**Q: Can't find "Serverless" option in Cosmos DB**
A: Select "Serverless" under "Capacity mode" dropdown. If not visible, change API to "Core (SQL)".

**Q: AI Search shows much higher cost**
A: Uncheck "Semantic Search" add-on (adds $500/month). Use only vector search.

**Q: Should I use Reserved Instances?**
A: For long-term (1-3 years), reserved capacity saves 15-30%. Not recommended for initial 3-month trial.

---

## Disclaimer

**Prices are subject to change.** Azure pricing is updated regularly. This guide uses rates from February 2026. Always verify current rates in the calculator.

**Usage estimates are projections.** Actual costs may vary based on:
- Real claim volumes (assumed 600 claims/day)
- Document sizes and page counts
- LLM token consumption (depends on prompt size)
- Data transfer patterns

**Monitor costs actively** using Azure Cost Management during the first month to validate estimates.

---

---

## 📊 MVP Pricing Summary - Final Recommendations

### Recommended MVP Configuration (100-200 claims/day)

**Total Monthly Cost: $243-306 USD**

#### Service Breakdown

| Service | Configuration | Monthly Cost | Why This Choice |
|---------|--------------|--------------|-----------------|
| **Azure OpenAI** | GPT-4o + text-embedding-ada-002 | **$90-95** | 50% cheaper than GPT-4 Turbo, excellent performance |
| **Azure AI Search** | Basic tier, 1 unit, 730 hrs | **$75** | Perfect for 10K documents, <1 qps |
| **Azure Cosmos DB** | Serverless mode | **$25** | Pay-per-use, scales automatically |
| **Blob Storage** | Standard Hot LRS, 3 GB | **$5** | Minimal docs storage |
| **Document Intelligence** | Prebuilt Read, 18K pages | **$18** | Text-only extraction (no tables) |
| **Language Service** | NER + Key Phrases, 6K records | **$12** | Entity extraction from claims |
| **Computer Vision** | Image Analysis, 3K images | **$3** | Damage assessment photos |
| **Monitoring** | Log Analytics, 2 GB logs | **$10** | Basic monitoring |
| **Data Transfer** | 5 GB egress | **$5** | Bandwidth costs |
| | | | |
| | **MVP TOTAL** | **$243-248/month** | **3-month cost: $729-744** |

### Scaling Timeline & Costs

```
Month 1-2: MVP Phase
├─ Cost: $243-306/month
├─ Volume: 100-200 claims/day
├─ Users: 5-10 internal testers
└─ Goal: Validate functionality, gather feedback

Month 3-4: Growth Phase  
├─ Cost: $374-506/month
├─ Volume: 300-400 claims/day
├─ Users: 20-30 users
└─ Goal: Pilot with select departments

Month 5-6: Pre-Production
├─ Cost: $506-681/month
├─ Volume: 500-600 claims/day
├─ Users: 50-75 users
└─ Goal: Prepare for full rollout

Month 7+: Production
├─ Cost: $681-951/month (based on AI model choice)
├─ Volume: 600+ claims/day
├─ Users: 100+ concurrent users
└─ Goal: Business-critical operations
```

### Cost per Claim Analysis

| Phase | Claims/Day | Monthly Claims | Monthly Cost | Cost per Claim |
|-------|-----------|----------------|--------------|----------------|
| **MVP** | 150 | 4,500 | $248 | **$0.055** (5.5 cents) |
| **Growth** | 350 | 10,500 | $431 | **$0.041** (4.1 cents) |
| **Production** | 600 | 18,000 | $681 | **$0.038** (3.8 cents) |

**ROI Insight:** At 3.8-5.5 cents per claim, if manual processing costs $2-5 per claim, the system pays for itself immediately.

---

## 🎯 Calculator Input Checklist for MVP

Use this exact checklist when entering values in Azure Pricing Calculator:

### 1. Azure OpenAI ✅
- [ ] Model: **GPT-4o** (not GPT-4 Turbo)
- [ ] Input tokens: **9,000,000** per month
- [ ] Output tokens: **3,000,000** per month
- [ ] Embedding model: **text-embedding-ada-002**
- [ ] Embedding tokens: **750,000** per month
- [ ] Region: **East US**
- **Expected cost: ~$90-95/month**

### 2. Azure AI Search ✅
- [ ] Service: **Azure Cognitive Search**
- [ ] Tier: **Basic** (not Standard S1)
- [ ] Units: **1**
- [ ] Hours: **730** (full month)
- [ ] Region: **East US**
- **Expected cost: $75/month**

### 3. Azure Cosmos DB ✅
- [ ] API: **Core (SQL)**
- [ ] Mode: **Serverless** (not Provisioned)
- [ ] Storage: **1 GB**
- [ ] Request Units: **60,000 RU/month** (low volume)
- [ ] Region: **East US** (single region)
- **Expected cost: $25/month**

### 4. Storage Account (Blob) ✅
- [ ] Type: **Block Blob**
- [ ] Performance: **Standard**
- [ ] Redundancy: **LRS** (Locally Redundant)
- [ ] Access tier: **Hot**
- [ ] Storage: **3 GB**
- [ ] Write operations: **6,000/month**
- [ ] Read operations: **1,000/month**
- **Expected cost: $5/month**

### 5. Document Intelligence ✅
- [ ] Model: **Read** (not Document or Layout)
- [ ] Pages: **18,000/month** (200 docs × 3 pages × 30 days)
- [ ] Pricing: **$1 per 1,000 pages**
- **Expected cost: $18/month**

### 6. Language Service ✅
- [ ] Feature: **Named Entity Recognition (NER)**
- [ ] Text records: **6,000/month** (200 claims × 30 days)
- [ ] Pricing: **$2 per 1,000 records**
- **Expected cost: $12/month**

### 7. Computer Vision ✅
- [ ] Feature: **Analyze** (Image Analysis)
- [ ] Transactions: **3,000/month** (100 images/day × 30)
- [ ] Pricing: **$1 per 1,000 transactions**
- **Expected cost: $3/month**

### 8. Monitor (Optional) ✅
- [ ] Log ingestion: **2 GB/month**
- [ ] Retention: **30 days**
- **Expected cost: $10/month**

### 9. Bandwidth (Optional) ✅
- [ ] Data transfer out: **5 GB/month**
- [ ] Region: **East US**
- **Expected cost: $5/month**

---

## ⚡ Quick Cost Comparison

| Scenario | Monthly | 3-Month | Per Claim | When to Use |
|----------|---------|---------|-----------|-------------|
| **MVP (Recommended)** | $248 | $744 | $0.055 | Starting out, POC, testing |
| **MVP + Higher Volume** | $306 | $918 | $0.051 | More active testing (200/day) |
| **Growth - Optimized** | $431 | $1,293 | $0.041 | Pilot with 350-400 claims/day |
| **Growth - With HA** | $606 | $1,818 | $0.034 | Need Standard S1 AI Search |
| **Production - Optimized** | $681 | $2,043 | $0.038 | Full scale with GPT-4o |
| **Production - Premium** | $951 | $2,853 | $0.053 | Full scale with GPT-4 Turbo |

**💡 Recommendation for 3-month MVP:** Start with **$248/month** configuration, monitor actual usage in month 1, adjust in month 2.

---

## 🚨 Common Calculator Mistakes to Avoid

### Mistake 1: Using Wrong OpenAI Model
❌ **Wrong:** GPT-4 Turbo at $10/$30 per 1M tokens  
✅ **Right:** GPT-4o at $5/$15 per 1M tokens (MVP choice)

**Impact:** Saves $270/month (50% reduction)

### Mistake 2: Using Wrong AI Search Tier
❌ **Wrong:** Standard S1 for MVP with 10K docs  
✅ **Right:** Basic tier (sufficient for <1M docs)

**Impact:** Saves $175/month (70% reduction)

### Mistake 3: Wrong Document Intelligence Model
❌ **Wrong:** Prebuilt Document at $10/1K pages  
✅ **Right:** Prebuilt Read at $1/1K pages (if no tables needed)

**Impact:** Saves $486/month (90% reduction)

### Mistake 4: Provisioned Cosmos DB
❌ **Wrong:** Provisioned 400 RU/s at ~$350/month  
✅ **Right:** Serverless at ~$25/month

**Impact:** Saves $325/month (93% reduction)

### Mistake 5: Adding Semantic Search
❌ **Wrong:** Enabling Semantic Search add-on  
✅ **Right:** Use only vector search (built-in)

**Impact:** Saves $500/month

### Mistake 6: Multiple Regions
❌ **Wrong:** Multi-region deployment  
✅ **Right:** Single region (East US) for MVP

**Impact:** Saves 50-100% on data transfer

### Mistake 7: Overestimating Volume
❌ **Wrong:** Planning for 600 claims/day from day 1  
✅ **Right:** Start with realistic 100-200/day, scale up

**Impact:** Saves $400-600/month initially

---

## 📈 When to Upgrade Services

### Basic → Standard S1 AI Search
**Upgrade when:**
- ✅ Document count exceeds 500,000
- ✅ Query load consistently >2-3 queries/second
- ✅ Need high availability (99.9% SLA)
- ✅ Need multiple replicas for scale

**Current MVP need:** 10K docs, 0.01 qps → Stay on Basic

### GPT-4o → GPT-4 Turbo
**Upgrade when:**
- ✅ Accuracy issues with complex policy interpretation
- ✅ Need highest reasoning quality for critical decisions
- ✅ Cost becomes less important than performance

**Current MVP need:** Testing functionality → Start with GPT-4o

### Serverless → Provisioned Cosmos DB
**Upgrade when:**
- ✅ Sustained >1000 claims/day
- ✅ Predictable high throughput
- ✅ Need reserved capacity discounts

**Current MVP need:** <200 claims/day → Stay Serverless

---

## 💰 Budget Planning Template

### 3-Month MVP Budget Request

```
Azure Claims RAG Bot - 3-Month MVP Budget

Phase: Proof of Concept & Validation
Duration: 3 months
Expected Volume: 100-200 claims/day

Monthly Breakdown:
├─ Month 1: $248 (setup + initial testing)
├─ Month 2: $275 (increased testing volume)
└─ Month 3: $306 (pilot expansion)

Total 3-Month Budget: $829
Average per month: $276

Contingency (20%): $166
Total Budget Request: $995

Expected Outcomes:
✅ Process 13,500-18,000 claims over 3 months
✅ Validate AI accuracy and performance
✅ Gather user feedback for improvements
✅ Generate ROI metrics for full deployment

Cost Comparison:
- Manual processing: $2-5 per claim × 15,000 = $30,000-75,000
- AI automated: $829 for 3 months
- Potential savings: $29,000-74,000 (96-99% reduction)
```

---

## 📞 Next Steps

### After Getting Budget Approval

1. **Export Calculator Estimate**
   - Click "Export" in Azure Pricing Calculator
   - Save as PDF: `Claims-Bot-MVP-Estimate-{date}.pdf`
   - Attach to budget approval email

2. **Set Up Azure Subscription**
   - Create Azure account (if new)
   - Set up billing with credit card or purchase order
   - Configure cost alerts at $200, $250, $300

3. **Start Resource Provisioning**
   - Follow `AZURE_PORTAL_SETUP_GUIDE.md`
   - Provision in this order:
     1. Resource Group
     2. Storage Account
     3. Azure OpenAI (request access first!)
     4. AI Search
     5. Cosmos DB
     6. Cognitive Services (Doc Intelligence, Language, Vision)

4. **Monitor Costs Daily (First Week)**
   - Check Azure Cost Management daily
   - Verify actual vs estimated costs
   - Adjust configurations if needed

5. **Review & Optimize (End of Month 1)**
   - Compare actual usage vs projections
   - Identify opportunities to optimize
   - Scale up or down based on real data

---

**Document Version:** 2.0 (Updated with MVP Focus)  
**Last Updated:** February 12, 2026  
**Maintained By:** Claims RAG Bot Team  
**Related Documents:**
- `AZURE_DEPLOYMENT_REQUIREMENTS.md` - Full technical specs
- `AZURE_PORTAL_SETUP_GUIDE.md` - Step-by-step provisioning
- `AZURE_SERVICES_GUIDE.md` - Service integration details

---

## 🎓 Key Takeaways

1. **Start Small:** MVP at $243-306/month is realistic and affordable
2. **Use GPT-4o:** 50% cost savings vs GPT-4 Turbo with excellent quality
3. **Basic AI Search is Enough:** For MVP with 10K documents and low query volume
4. **Serverless Everything:** Cosmos DB and consumption-based pricing reduces risk
5. **Monitor & Adjust:** Check costs weekly, optimize based on actual usage
6. **Scale Gradually:** Move from MVP ($248) → Growth ($431) → Production ($681) as demand proves out

**Ready to start?** Open the Azure Pricing Calculator and use the checklist above! 🚀
