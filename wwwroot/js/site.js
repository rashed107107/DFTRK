// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Professional Confirmation Modal Functions
let confirmCallback = null;

/**
 * Show a professional confirmation modal instead of JavaScript confirm()
 * @param {string} message - The confirmation message to display
 * @param {function} callback - The function to call if user confirms
 * @param {string} title - Optional title for the modal (default: "Confirm Action")
 * @param {string} confirmText - Optional text for confirm button (default: "Confirm")
 * @param {string} confirmClass - Optional CSS class for confirm button (default: "btn-danger")
 */
function showConfirmation(message, callback, title = "Confirm Action", confirmText = "Confirm", confirmClass = "btn-danger") {
    // Update modal content
    document.getElementById('confirmationModalLabel').textContent = title;
    document.getElementById('confirmationMessage').textContent = message;
    
    const confirmBtn = document.getElementById('confirmActionBtn');
    confirmBtn.textContent = confirmText;
    confirmBtn.className = `btn ${confirmClass}`;
    
    // Store the callback
    confirmCallback = callback;
    
    // Show the modal
    const modal = new bootstrap.Modal(document.getElementById('confirmationModal'));
    modal.show();
}

/**
 * Enhanced confirmation for delete actions
 * @param {string} itemType - What is being deleted (e.g., "order", "category", "product")
 * @param {function} callback - The function to call if user confirms
 */
function showDeleteConfirmation(itemType, callback) {
    showConfirmation(
        `Are you sure you want to delete this ${itemType}? This action cannot be undone.`,
        callback,
        "Confirm Deletion",
        "Delete",
        "btn-danger"
    );
}

/**
 * Enhanced confirmation for cancel actions
 * @param {string} itemType - What is being cancelled (e.g., "order")
 * @param {function} callback - The function to call if user confirms
 */
function showCancelConfirmation(itemType, callback) {
    showConfirmation(
        `Are you sure you want to cancel this ${itemType}? This action cannot be undone.`,
        callback,
        "Confirm Cancellation",
        "Cancel " + itemType.charAt(0).toUpperCase() + itemType.slice(1),
        "btn-warning"
    );
}

/**
 * Enhanced confirmation for completion actions
 * @param {string} action - The action being completed (e.g., "delivery", "order")
 * @param {function} callback - The function to call if user confirms
 */
function showCompletionConfirmation(action, callback) {
    showConfirmation(
        `Mark this ${action} as complete?`,
        callback,
        "Confirm Completion",
        "Complete",
        "btn-success"
    );
}

// Handle confirm button click
document.addEventListener('DOMContentLoaded', function() {
    const confirmBtn = document.getElementById('confirmActionBtn');
    if (confirmBtn) {
        confirmBtn.addEventListener('click', function() {
            if (confirmCallback) {
                confirmCallback();
                confirmCallback = null;
            }
            // Hide the modal
            const modal = bootstrap.Modal.getInstance(document.getElementById('confirmationModal'));
            if (modal) {
                modal.hide();
            }
        });
    }
});

/**
 * Helper function to replace onclick="return confirm(...)" usage
 * Call this function instead of using confirm() directly
 * @param {string} message - The confirmation message
 * @param {function} action - The action to perform if confirmed
 * @param {Event} event - The event object (to prevent default)
 */
function confirmAction(message, action, event) {
    event.preventDefault();
    showConfirmation(message, action);
    return false;
}
