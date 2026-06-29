// Utility functions for EMS dashboard

// Loading state management - prevent double submissions
const LoadingState = {
    isLoading: false,

    show: function(message = 'Loading...') {
        this.isLoading = true;
        const overlay = document.createElement('div');
        overlay.id = 'loading-overlay';
        overlay.style.cssText = `
            position: fixed;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            background: rgba(15, 23, 42, 0.8);
            display: flex;
            align-items: center;
            justify-content: center;
            z-index: 9999;
        `;
        overlay.innerHTML = `
            <div style="text-align: center; color: #E2E8F0;">
                <div style="font-size: 24px; margin-bottom: 10px;">⏳</div>
                <p>${message}</p>
            </div>
        `;
        document.body.appendChild(overlay);
    },

    hide: function() {
        this.isLoading = false;
        const overlay = document.getElementById('loading-overlay');
        if (overlay) overlay.remove();
    }
};

// Form validation utilities
const FormValidation = {
    validateRequired: function(value) {
        return value && value.trim().length > 0;
    },

    validateEmail: function(email) {
        const re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return re.test(email);
    },

    validateNumber: function(value) {
        return !isNaN(parseFloat(value)) && isFinite(value);
    },

    showError: function(fieldId, message) {
        const field = document.getElementById(fieldId);
        if (field) {
            field.classList.add('is-invalid');
            const errorDiv = document.createElement('div');
            errorDiv.className = 'invalid-feedback';
            errorDiv.textContent = message;
            field.parentNode.appendChild(errorDiv);
        }
    },

    clearErrors: function() {
        document.querySelectorAll('.is-invalid').forEach(el => el.classList.remove('is-invalid'));
        document.querySelectorAll('.invalid-feedback').forEach(el => el.remove());
    }
};

// Safe navigation helper
function safeValue(obj, defaultValue = '—') {
    return obj !== null && obj !== undefined ? obj : defaultValue;
}
