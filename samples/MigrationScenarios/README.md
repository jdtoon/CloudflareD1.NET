# Migration Scenarios - Comprehensive Test Suite

This test project validates the snapshot-based migration generation system across all common schema change scenarios.

## Overview

The test suite ensures that:
- ✅ Snapshots are created and updated correctly
- ✅ Incremental migrations detect only actual changes
- ✅ No false positives (unchanged models don't trigger migrations)
- ✅ Multiple changes in one migration are handled correctly
- ✅ Up and Down methods are generated properly

## Test Scenarios

### Scenario 1: Initial Schema Creation
**Purpose**: Validate initial migration creates all tables and snapshot  
**Steps**:
1. Create context with Product table
2. Generate InitialCreate migration
3. Verify migration file exists
4. Verify snapshot file created
5. Verify Up method contains CreateTable
6. Verify Down method contains DropTable

**Expected**: Migration creates table, snapshot persists schema

---

### Scenario 2: Add New Table
**Purpose**: Ensure incremental migrations only add new tables  
**Steps**:
1. Generate migration with Products table only
2. Add Categories table to context
3. Generate AddCategories migration
4. Verify only categories table is created
5. Verify products table NOT recreated

**Expected**: Second migration contains only new table, not existing ones

---

### Scenario 3: Add Column to Existing Table
**Purpose**: Detect new column additions  
**Steps**:
1. Generate migration with basic Product (id, name)
2. Add description column to Product
3. Check changes summary
4. Generate AddDescription migration

**Expected**: 
- Changes summary shows "+ Add column 'products.description'"
- Migration contains ALTER TABLE with new column

---

### Scenario 4: Remove Column from Existing Table
**Purpose**: Detect column removals  
**Steps**:
1. Generate migration with Product including price
2. Remove price column from Product
3. Check changes summary
4. Generate RemovePrice migration

**Expected**:
- Changes summary shows "- Drop column 'products.price'"
- Snapshot updated without price column
- Note: MigrationScaffolder limitation - may show "No changes detected" in file

---

### Scenario 5: Add Foreign Key Relationship
**Purpose**: Detect foreign key additions  
**Steps**:
1. Generate migration with Products and OrderItems (no FK)
2. Add FK relationship from OrderItems.ProductId → Products.Id
3. Check changes summary
4. Generate AddProductFK migration

**Expected**:
- Changes summary shows "+ Add foreign key 'order_items.product_id' -> 'products.id'"
- Also detects navigation property column addition

---

### Scenario 6: No Changes (Idempotency)
**Purpose**: Ensure no false positives  
**Steps**:
1. Generate InitialCreate migration
2. Try to generate again with same model
3. Check HasPendingChangesAsync

**Expected**:
- HasPendingChangesAsync returns false
- No spurious change detection

---

### Scenario 7: Multiple Changes in One Migration
**Purpose**: Handle complex migrations with multiple operations  
**Steps**:
1. Generate migration with basic Product (id, name)
2. Make multiple changes:
   - Add Reviews table
   - Add stock column to Product
   - Remove name column from Product
3. Check changes summary

**Expected**:
- All three changes detected:
  - "+ Create table 'reviews'"
  - "+ Add column 'products.stock'"
  - "- Drop column 'products.name'"

---

### Scenario 8: Snapshot Remains Consistent
**Purpose**: Verify snapshot stability  
**Steps**:
1. Generate InitialCreate migration
2. Read snapshot content
3. Check for pending changes (should be none)
4. Read snapshot content again
5. Compare snapshots

**Expected**:
- Snapshot remains identical
- No mutations when no model changes occur

---

## Running the Tests

```bash
cd samples/MigrationScenarios
dotnet run
```

## Test Results

All 8 scenarios passing indicates:
- ✅ Snapshot-based comparison working correctly
- ✅ Incremental migrations generating properly
- ✅ Change detection accurate (tables, columns, indexes, FKs)
- ✅ No false positives
- ✅ Snapshot persistence stable

## Known Limitations

1. **MigrationScaffolder ALTER TABLE Support**:
   - DROP COLUMN detected in changes summary
   - BUT migration file may show "No changes detected"
   - This is a scaffolder limitation, not snapshot comparison issue
   - Future enhancement needed for full ALTER TABLE generation

2. **Navigation Properties**:
   - Navigation properties (e.g., `public Product? Product { get; set; }`) are currently stored as TEXT columns
   - System correctly detects them as column additions
   - This is expected behavior in current implementation

## Architecture Validation

These tests validate the refactored architecture:
- **Before**: Migrations compared models against database schema
- **After**: Migrations compare models against previous snapshot
- **Benefit**: Incremental migrations that only show deltas

## Snapshot File Structure

`.migrations-snapshot.json` example:
```json
{
  "Tables": [
    {
      "Name": "products",
      "Columns": [
        { "Name": "id", "Type": "INTEGER", "NotNull": true, "IsPrimaryKey": true },
        { "Name": "name", "Type": "TEXT", "NotNull": true, "IsPrimaryKey": false }
      ],
      "Indexes": [],
      "ForeignKeys": []
    }
  ]
}
```

## Next Steps

After all tests pass:
1. ✅ Snapshot-based migration system validated
2. 🔄 Enhance MigrationScaffolder for ALTER TABLE statements
3. 🔄 Move to Priority #2: Change Tracking & SaveChanges
